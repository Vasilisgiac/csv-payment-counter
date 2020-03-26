using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using System.IO;
using System.Globalization;
using RestSharp;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Data.SQLite;
using Dapper;

namespace ConsoleApp1
{
    class Program
    {
        /// <summary>
        /// Κύρια μέθοδο
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            var records = new List<Records>(); //Δημιουργούμε μια λίστα με αντικείμενα τύπου Records
            var records2 = new List<Records2>(); //Δημιουργούμε μια λίστα με αντικείμενα τύπου Records2
            string path = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)))));

            using (var reader = new StreamReader(path + @"\TEST\PF.csv")) //Δημιουργούμε ένα reader με το path του csv αρχείου
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture)) //Δημιουργούμε ένα Csvreader από το πακέτο Csvhelper, σύμφωνα με το προηγούμενο stream reader
            {
                csv.Configuration.Delimiter = ";"; //Θέτουμε ρύθμιση να χωρίζει το csvreader κάθε πεδίο οταν συναντα τον χαρακτήρα ';'
                records = csv.GetRecords<Records>().ToList(); //Καλούμε την μέθοδο GetRecords από το πακέτο Csvhelper και εκχωρούμε τα δεδομένα σε μορφή Records μέσα σε λίστα
            }
            
            CheckRecords(records, records2); //Καλούμε την μέθοδο CheckRecords για να ελέγξουμε αν τα δεδομένα του csv αρχείου τηρούν τα πρότυπα που θέλουμε για τον πίνακα της βάσης

            CreateandInsert(records2); //Καλούμε την μέθοδο CreateandInsert για να δημιουργήσουμε και να εκχωρίσουμε τα δεδομένα στην Sqlite βάση

            Printpayments(records2); //Καλούμε την μέθοδο Printpayments για να εκτυπώσουμε αθροιστικά τις πληρωμές που έγιναν σε ευρώ για κάθε διαφορετικό νόμισμα

            Console.WriteLine("Press any key to continue..");
            Console.ReadLine();
        }

        /// <summary>
        /// Μέθοδος για να εκχωρίσουμε τις κακές εγγραφές του csv αρχείου σε ένα διαφορετικό αρχείο τύπου text
        /// </summary>
        /// <param name="records"></param>
        /// <param name="x"></param>
        static void WriteBadlines(List<Records> records, int x)
        {

            string path = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)))));
            path += @"\TEST\bad.txt";

            TextWriter tw = new StreamWriter(path, true); //Δημιουργία αρχείου και writer
            tw.WriteLine(records[x].ACCOUNT_NUMBER + ';' + records[x].DESCRIPTION + ';' + records[0].PAYDATE + ';' + records[0].PAY_AMOUNT + ';' + records[0].BALANCE + ';' + records[0].CURRENCY); //Εκχώρηση καταγραφής
            tw.Close(); //Κλείσιμο writer 
        }

        /// <summary>
        /// Mέθοδος για να ελέγξουμε αν τα δεδομένα του csv αρχείου τηρούν τα πρότυπα που θέλουμε για τον πίνακα της βάσης
        /// </summary>
        /// <param name="records"></param>
        /// <param name="records2"></param>
        static void CheckRecords(List<Records> records, List<Records2> records2)
        {
            for (int x = 0; x < records.Count; x++)
            {
                long d1;
                decimal d2;
                DateTime d3;
                Records2 record = new Records2();
                if (long.TryParse(records[x].ACCOUNT_NUMBER, out d1)) //Έλεγχος αν ο αριθμός λογαριασμού περιέχει μόνο νούμερα
                {
                    record.ACCOUNT_NUMBER = d1; //Αποθηκεύουμε τον αριθμό λογαριασμού
                }
                else
                {
                    WriteBadlines(records, x); //Εκχωρούμε την κακή καταγραφή
                    continue;
                }

                if (records[x].DESCRIPTION.Length <= 50) //Έλεγχος αν η περιγραφή πληρωμής περιέχει μέχρι πενήντα χαρακτήρες
                {
                    record.DESCRIPTION = records[x].DESCRIPTION; //Αποθηκεύουμε την περιγραφή πληρωμής
                }
                else
                {

                    WriteBadlines(records, x); //Εκχωρούμε την κακή καταγραφή
                    continue;
                }



                if (records[x].CURRENCY.Length == 3)  //Έλεγχος αν η ονομασία νομίσματος περιέχει ακριβώς τρεις χαρακτήρες
                {
                    var currencies = GetCurrencies(); //Μέθοδος που επιστρέφει όλες τις πιθανές ονομασίες νομισμάτων
                    if (currencies.Contains(records[x].CURRENCY)) //Έλεγχος αν η ονομασία νομίσματος αντιστοιχεί σε κάποια πραγματική
                    { record.CURRENCY = records[x].CURRENCY; } //Αποθηκεύουμε την ονομασία νομίσματος
                    else 
                    { 
                        WriteBadlines(records, x); //Εκχωρούμε την κακή καταγραφή
                        continue;
                    }
                }
                else
                {

                    WriteBadlines(records, x); //Εκχωρούμε την κακή καταγραφή
                    continue;
                }

                if (decimal.TryParse(records[x].PAY_AMOUNT, out d2)) //Έλεγχος αν το ποσόο πληρώμης είναι σε δεκαδική μορφή
                {
                    string[] a = records[x].PAY_AMOUNT.ToString().Split('.');
                    if (a[0].Length <= 10 && a[1].Length == 2) //Έλεγχος αν το πόσο πληρώμης είναι σε δεκαδική μορφή με 10 ψηφία πρίν την υποδιαστολή και 2 μετά
                    {
                        record.PAY_AMOUNT = d2; //Αποθηκεύουμε το ποσό πληρωμής
                    }
                    else 
                    {
                        WriteBadlines(records, x); //Εκχωρούμε την κακή καταγραφή
                        continue;
                    }
                }
                else
                {
                    WriteBadlines(records, x); //Εκχωρούμε την κακή καταγραφή
                    continue;
                }

                if (decimal.TryParse(records[x].BALANCE, out d2)) //Έλεγχος αν το ποσό υπολοίπου είναι σε δεκαδική μορφή
                {
                    string[] a = records[x].BALANCE.ToString().Split('.');
                    if (a[0].Length <= 10 && a[1].Length == 2) //Έλεγχος αν το πόσο υπολοίπου είναι σε δεκαδική μορφή με 10 ψηφία πρίν την υποδιαστολή και 2 μετά
                    {
                        record.BALANCE = d2; //Αποθηκεύουμε το ποσό υπολοίπου
                    }
                    else
                    {
                        WriteBadlines(records, x); //Εκχωρούμε την κακή καταγραφή
                        continue;
                    }
                }
                else
                {
                    WriteBadlines(records, x); //Εκχωρούμε την κακή καταγραφή
                    continue;
                }

                if (DateTime.TryParseExact(records[x].PAYDATE, "yyyy-MM-dd", null, DateTimeStyles.None, out d3)) //Έλεγχος αν η ημερομηνία πληρωμής είναι σε μορφή χρόνος(4 ψηφία)-μήνας(2 ψηφία)-μέρα(2 ψηφία)
                {
                    record.PAY_DATE = d3; //Αποθηκεύουμε την ημερομηνία πληρωμής
                }
                else
                {
                    WriteBadlines(records, x); //Εκχωρούμε την κακή καταγραφή
                    continue;
                }

                if (record.CURRENCY != "EUR") //Έλεγχος αν η ονομασία νομίσματος της καταγραφής είναι διαφορετική απο του ευρώ
                {
                    if (Conversioncurrency(record.PAY_DATE.ToString("yyyy-MM-dd"), record.CURRENCY, record.PAY_AMOUNT, record.BALANCE).Count != 0) //Μετατρέπουμε το ποσό πληρωμής και το υπόλοιπο σε ευρώ βάση ισοτιμίας της ημέρας που έγινε η πληρωμή
                    {
                        List<decimal> conversions = Conversioncurrency(record.PAY_DATE.ToString("yyyy-MM-dd"), record.CURRENCY, record.PAY_AMOUNT, record.BALANCE);
                        record.PAY_AMOUNT_CURRENCY = conversions[0];
                        record.BALANCE_CURRENCY = conversions[1];
                    }
                    else 
                    {
                        Console.WriteLine("error at currency conversion"); //Εκτύπωση ειδοποίησης σε περίπτωση λάθους κατά την μετατροπή
                    }
                }
                else //Το ποσό πληρωμής και το υπόλοιπο μένουν ίδια καθώς είναι σε μορφή ευρώ
                {
                    record.PAY_AMOUNT_CURRENCY = record.PAY_AMOUNT;
                    record.BALANCE_CURRENCY = record.BALANCE;
                }
                records2.Add(record); //Εκχωρούμε την καταγραφη σε λίστα, εφόσον έχει περάσει όλους τους ελέγχους πιο πριν

            }
        }

        /// <summary>
        /// Μέθοδος για την συλλογή των πιθανών ονομασιών νομισμάτων με βάση το api "exchangeratesapi"
        /// </summary>
        /// <returns></returns>
        static List<string> GetCurrencies()
        {
            var client = new RestClient("https://api.exchangeratesapi.io"); //Δημιουργία σύνδεσης με το api
            var request = new RestRequest("latest", Method.GET); //Δημιουργία request μεθόδου GET 
            IRestResponse response = client.Execute(request); //Εκτέλεση του request
            var content = response.Content; //Αποθήκευση απάντησης του request (default απάντηση από api σε JSON μορφή)
            var jobject = JObject.Parse(content); //Μετατροπή της JSON απάντησης σε μορφή JObject του πακέτου Json.NET
            List<string> properties = new List<string>(); //Δημιουργία λίστας με string
            foreach (JProperty property in jobject.GetValue("rates")) //Προσπέλαση των διαφορετικών ισοτιμιών
            {
                properties.Add(property.Name); //Αποθήκευση της κάθε ονομασίας νομίσματος
            }
            properties.Add(jobject.GetValue("base").ToString()); //Αποθήκευση της ονομασίας του ευρώ
            return properties; //Επιστροφή λίστας ονομασιών
        }

        /// <summary>
        /// Μέθοδος μετατροπής του ποσού πληρωμής και του υπολοίπου σε ευρώ βάση ισοτιμίας της ημέρας που έγινε η πληρωμή
        /// </summary>
        /// <param name="date"></param>
        /// <param name="currency"></param>
        /// <param name="amount"></param>
        /// <param name="balance"></param>
        /// <returns></returns>
        static List<decimal> Conversioncurrency(string date, string currency, decimal amount, decimal balance)
        {
            var client = new RestClient("https://api.exchangeratesapi.io");//Δημιουργία σύνδεσης με το api
            var request = new RestRequest("{date}", Method.GET);//Δημιουργία request μεθόδου GET με παράμετρο
            request.AddUrlSegment("date", date); //Εκχώρηση της ημερομηνίας πληρωμής στην παράμετρου του request
            request.AddParameter("base", currency); //Προσθήκη επιπλέον παραμέτρου, προσθέτουμε το νόμισμα με βάση το οποίο θα γίνει η ισοτιμία σε ευρώ
            request.AddParameter("symbols", "EUR"); //Προσθήκη επιπλέον παραμέτρου, προσθέτουμε έναντι ποιού νομίσματος θέλουμε να γίνει η ισοτιμία
            IRestResponse response = client.Execute(request); //Εκτέλεση του request
            var content = response.Content; //Αποθήκευση απάντησης του request (default απάντηση από api σε JSON μορφή)
            var jobject = JObject.Parse(content); //Μετατροπή της JSON απάντησης σε μορφή JObject του πακέτου Json.NET
            decimal conversion;
            List<decimal> conversions = new List<decimal>(); //Δημιουργία λίστας με δεκαδικούς
            foreach (JProperty property in jobject.GetValue("rates"))//Προσπέλαση των διαφορετικών ισοτιμιών
            {
                if (decimal.TryParse(property.Value.ToString(), out conversion)) //Έλεγχος οτι οι ισοτιμίες είναι σε δεκαδική μορφή
                {
                    conversions.Add(Math.Round(amount * conversion, 2)); //Μετατροπή ποσού πληρωμής σε ευρώ
                    conversions.Add(Math.Round(balance * conversion, 2)); //Μετατροπή υπολοίπου σε ευρώ
                    return conversions; //Επιστροφή λίστας με τις μετατροπές
                }
                else { Console.WriteLine("error at currency conversion"); //Εκτύπωση μηνύματος σε περίπτωση που οι ισοτιμίες δεν είναι σε δεκαδική μορφή
                    return null;
                }
            }
            return conversions; //Επιστροφή λίστας με τις μετατροπές
        }

        /// <summary>
        /// Μέθοδος για να εκτυπώσουμε αθροιστικά τις πληρωμές που έγιναν σε ευρώ για κάθε διαφορετικό νόμισμα
        /// </summary>
        /// <param name="records2"></param>
        static void Printpayments(List<Records2> records2)
        {
            List<Payments> payments = new List<Payments>(); //Δημιουργία λίστας τύπου payments
            
            for (int x = 0; x < records2.Count; x++) //Προσπέλαση κάθε καταγραφής
            {
                if (!payments.Exists(e => e.CURRENCY == records2[x].CURRENCY)) //Έλεχος αν η ονομασία νομίσματος υπάρχει ήδη στην λίστα
                {
                    Payments payment = new Payments();
                    payment.CURRENCY = records2[x].CURRENCY; //Αποθήκευση ονομασίας νομίσματος
                    payment.PAY_AMOUNT = records2[x].PAY_AMOUNT_CURRENCY; //Αποθήκευση πληρωμής σε μορφή ευρώ
                    payments.Add(payment); //Προσθήκη πληρωμής σε λίστα
                }
                else //Αν η ονομασία νομίσματος υπάρχει ήδη
                {
                    int index;
                    index = payments.FindIndex(e => e.CURRENCY.Contains(records2[x].CURRENCY)); //Αριθμός στοιχείου λίστας με την ονομασία νομίσματος της καταγραφής
                    payments[index].PAY_AMOUNT += records2[x].PAY_AMOUNT_CURRENCY; //Αθροίζουμε το πόσο στο ήδη υπάρχον

                }
                
            }
            foreach (var payment2 in payments) //Εκτύπωση πληρωμών στην κονσόλα
            {
                Console.OutputEncoding = System.Text.Encoding.Default;
                Console.WriteLine(payment2 + " €");
            }
        }

        /// <summary>
        /// Μέθοδος για να δημιουργήσουμε και να εκχωρίσουμε τα δεδομένα στην Sqlite βάση
        /// </summary>
        /// <param name="records2"></param>
        static void CreateandInsert(List<Records2> records2)
        {
            string path = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)));
            path += @"\sqlite.db";

            if (!File.Exists(path)) //Έλεγχος αν η βάση υπάρχει ήδη
            {
                SQLiteConnection.CreateFile(path); //Δημιουργία Βάσης
            }
            var connectionString = "Data Source="+ path + ";Version=3;"; //Παράμετροι σύνδεσης
            SQLiteConnection conn = new SQLiteConnection(connectionString); //Δημιουργία σύνδεσης
            conn.Open(); //Έναρξη σύνδεσης
            //Πρόταση δημιουργίας πίνακα MY_PAYMENTS σε μορφή SQL
            string sql = @"CREATE TABLE IF NOT EXISTS MY_PAYMENTS (
                            CASE_ID INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                            TIME_STAMP TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                            ACCOUNT_NUMBER NUMBER NOT NULL, 
                            DESCRIPTION TEXT(50),
                            PAY_DATE DATE NOT NULL,
                            PAY_AMOUNT NUMBER(10,2) NOT NULL,
                            BALANCE NUMBER(10,2) NOT NULL, 
                            CURRENCY TEXT(3) NOT NULL, 
                            BALANCE_CURRENCY NUMBER(10,2), 
                            PAY_AMOUNT_CURRENCY NUMBER(10,2))";
            
            conn.Execute(sql); //Εκτέλεση SQL πρότασης
            //Πρόταση εκχώρησης καταγραφής στην βάση δεδομένων σε μορφή SQL
            sql = @"INSERT INTO MY_PAYMENTS (TIME_STAMP, ACCOUNT_NUMBER, DESCRIPTION, PAY_DATE, PAY_AMOUNT, BALANCE, CURRENCY, BALANCE_CURRENCY, PAY_AMOUNT_CURRENCY)
                            VALUES
                            (DATETIME('NOW', 'LOCALTIME'), @ACCOUNT_NUMBER, @DESCRIPTION, @PAY_DATE, @PAY_AMOUNT, @BALANCE, @CURRENCY, @BALANCE_CURRENCY, @PAY_AMOUNT_CURRENCY)";
            
                conn.Execute(sql, records2); //Εκτέλεση SQL πρότασης
            
            
        }

    }
    
}
