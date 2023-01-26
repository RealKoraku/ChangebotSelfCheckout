using System.Diagnostics;

namespace NewerKiosk;

public struct Currency {
    public string type;
    public decimal value;
    public int drawerAmt;

    public Currency(string atype, decimal avalue, int aamt) {
        type = atype;
        value = avalue;
        drawerAmt = aamt;
    }
}
internal class Program {
    public static bool paymentComplete;
    static void Main(string[] args) {
        decimal drawerTotal = 0.00m;

        Currency[] cashDrawer = InitializeDrawer();
        drawerTotal = CheckDrawer(cashDrawer);

        while (true) {

            Console.ForegroundColor = ConsoleColor.White;

            string dateString = GetDate();
            string timeString = GetTime();

            decimal total = 0.00m;
            decimal change = 0.00m;
            decimal CBamount = 0.00m;
            decimal dispensed = 0.00m;
            decimal cashIntakeTotal = 0.00m;
            decimal cardAmount = 0.00m;
            int type;
            string cardStr;
            string cardType = "None";
            bool validCard;
            string console;
            bool parser;
            decimal bankAccepted = 0.00m;
            string[] bankResponse = new string[2];
            string transactionNo = "";

            paymentComplete = false;

            Console.WriteLine("Welcome to Changebot! 2023 NoHomoSapiens\n");

            int[] intake = new int[cashDrawer.Length];

            total = ScanItems();
            total = decimal.Round(total, 2, MidpointRounding.AwayFromZero);
            DisplayTotal(total);

            while (paymentComplete == false) {
                type = SelectPaymentType();

                if (type == 1) {
                    change = InsertCash(total, cashDrawer, intake);
                    cashIntakeTotal = cashIntake(intake, cashDrawer);
                    dispensed = DispenseChange(CBamount, change, cashDrawer, intake);
                    paymentComplete = true;

                } else if (type == 2) {
                    cardStr = InsertCard(total);
                    validCard = IsValidCard(cardStr);
                    cardType = CardType(cardStr);
                    GreenText(cardType);
                    CBamount = CashBack(total, cardStr, validCard, cashDrawer);

                    if (validCard) {

                        bankResponse = MoneyRequest(cardStr, total);

                        if (bankResponse[1] == "declined") {

                            DarkRedText("Bank declined transaction.");
                            
                            CBamount = 0;
                            cardAmount = 0;
                            total = PaymentError(total, validCard, cashDrawer);

                        } else if (bankResponse[1] == total.ToString()) {
                            Console.WriteLine($"Bank accepted {total:C}");
                            cardAmount = total;
                            paymentComplete = true;

                        } else {
                            Console.WriteLine($"Bank accepted {bankResponse[1]:C}.");
                            CBamount = 0;

                            parser = decimal.TryParse(bankResponse[1], out bankAccepted);

                            total = total - bankAccepted;

                            cardAmount = bankAccepted;
                            DisplayTotal(total);

                            do {
                                console = Input("Finish payment in cash? (y/n)");
                            } while (console.ToLower() != "y" && console.ToLower() != "n");

                            if (console.ToLower() == "y") {
                                change = InsertCash(total, cashDrawer, intake);
                                cashIntakeTotal = cashIntake(intake, cashDrawer);
                                paymentComplete = true;
                            } else {
                                Console.WriteLine("Transaction cancelled.");
                                total = 0;
                                paymentComplete = true;
                            }
                        }
                    }
                    if (paymentComplete) {
                        dispensed = DispenseChange(CBamount, change, cashDrawer, intake);
                    }
                }
            }

            cashDrawer = RefreshDrawer(cashDrawer, intake);
            drawerTotal = CheckDrawer(cashDrawer);
            transactionNo = TransactionNumber();

            LaunchLogger(transactionNo, dateString, timeString, cashIntakeTotal, cardType, cardAmount, dispensed);
        }
    }//end main

        #region drawer
        static Currency[] InitializeDrawer() {
            Currency[] cashDrawer = new Currency[12];

            cashDrawer[0] = new("penny", 0.01m, 300);
            cashDrawer[1] = new("nickel", 0.05m, 200);
            cashDrawer[2] = new("dime", 0.10m, 100);
            cashDrawer[3] = new("quarter", 0.25m, 100);
            cashDrawer[4] = new("half-dollar", 0.50m, 0);
            cashDrawer[5] = new("one", 1.00m, 200);
            cashDrawer[6] = new("two", 2.00m, 0);
            cashDrawer[7] = new("five", 5.00m, 100);
            cashDrawer[8] = new("ten", 10.00m, 50);
            cashDrawer[9] = new("twenty", 20.00m, 40);
            cashDrawer[10] = new("fifty", 50.00m, 40);
            cashDrawer[11] = new("hundred", 100.00m, 20);

            return cashDrawer;
        }

        //static Currency CreateCurrency(string type, decimal value, int amt) {
        //    Currency denom = new Currency();

        //    denom.type = type;
        //    denom.value = value;
        //    denom.drawerAmt = amt;

        //    return denom;
        //}
        #endregion

        #region purchase

        static decimal ScanItems() {
            decimal itemPrice;
            decimal total = 0;
            bool scanDone = false;
            string console;
            bool parser;

            for (int item = 0; scanDone == false; item++) {
                do {
                    console = Input($"Item #{item + 1}    $");

                    if ((console == "") && (item > 0)) {
                        scanDone = true;
                    }

                    parser = decimal.TryParse(console, out itemPrice);

                    if (itemPrice < 0) {
                        parser = false;
                    }

                } while (parser == false && scanDone == false);

                total = total + itemPrice;
            }
            return total;
        }

        static void DisplayTotal(decimal total) {
             GreenText($"\nTotal      {total:C}");
        }

        static int SelectPaymentType() {
            string console;
            bool parser;
            int type;

            do {
                console = Input("\nSelect payment type:\nCash (1)\nCard (2)\n");
                parser = int.TryParse(console, out type);

            } while ((parser == false) || (type != 1 && type != 2));

            return type;
        }

        #endregion purchase

        #region cash

        static decimal InsertCash(decimal total, Currency[] cashDrawer, int[] intake) {
            string console;
            bool parser;
            decimal payment;
            decimal change;
            bool validCurrency;

            for (int pay = 0; total > 0.00m; pay++) {

                do {
                    console = Input($"\nPayment {pay + 1}  $");
                    parser = decimal.TryParse(console, out payment);

                    validCurrency = IsValidCurrency(payment, cashDrawer);

                } while (parser == false || validCurrency == false);

                total = total - payment;

                for (int drawer = 0; drawer < cashDrawer.Length; drawer++) {
                    if (payment == cashDrawer[drawer].value) {
                        intake[drawer]++;
                    }
                }

                if (total > 0.00m) {
                Console.WriteLine($"Remaining  {total:C}");
                }
            }
            decimal endTotal = total * -1;

            GreenText($"\nChange     {endTotal:C}");

            return endTotal;
        }

        static decimal cashIntake(int[] intake, Currency[] cashDrawer) {
            decimal cashIntakeTotal = 0.00m;

            for (int i = 0; i < intake.Length; i++) {
                cashIntakeTotal = cashIntakeTotal + (intake[i] * cashDrawer[i].value);
            }
            return cashIntakeTotal;
        }

        static bool IsValidCurrency(decimal payment, Currency[] cashDrawer) {
            bool validCurrency = false;

            for (int drawer = 0; drawer < cashDrawer.Length; drawer++) {
                if (payment == cashDrawer[drawer].value) {
                    validCurrency = true;
                    return validCurrency;
                }
            }
            return validCurrency;
        }

        #endregion

        #region card

        static string InsertCard(decimal total) {
            string console;
            bool parser;
            Int64 cardNo;
            string cardStr;
            int[] card = new int[16];
            bool validCard;

            do {
                console = Input("\nPlease enter your 16-digit card number: ");
                parser = Int64.TryParse(console, out cardNo);
            } while (parser == false);

            cardStr = cardNo.ToString();

            return cardStr;
        }

        static string CardType(string cardStr) {
            string cardType = "";

            if (cardStr[0] == '3') {
                cardType = "American Express";
            } else if (cardStr[0] == '4') {
                cardType = "Visa";
            } else if (cardStr[0] == '5' || cardStr[0] == '2') {
                cardType = "Mastercard";
            } else if (cardStr[0] == '6') {
                cardType = "Discover";
            } else {
                cardType = "Other/Unknown";
            }
            return cardType;
        }

        static bool IsValidCard(string cardStr) {
            string console;
            double numSum = 0;
            double numProduct = 0;
            int aNum;
            int[] card = new int[cardStr.Length];
            string productStr;
            bool isValidCard = false;
            int finalNum;

            for (int i = 0; i < cardStr.Length; i++) {
                aNum = (int)char.GetNumericValue(cardStr[i]);
                card[i] = aNum;

                numSum = numSum + card[i];
            }

            numProduct = numSum / 10;
            numProduct = numProduct + 0.1;
            productStr = numProduct.ToString();

            isValidCard = int.TryParse(productStr, out finalNum);

            return isValidCard;
        }

        static decimal CashBack(decimal total, string cardStr, bool validCard, Currency[] cashDrawer) {
            string console;
            bool parser;
            bool parser2;
            bool request = false;
            int amount = 0;
            decimal withdrawal;
            decimal drawerTotal;
            string account_number = cardStr;
            string[] bankInfo;
            int[] intake = { 0 };

            do {
                console = Input("\nWould you like cash-back? (y/n)");
            } while (console.ToLower() != "n" && console.ToLower() != "y");

            if (console.ToLower() == "y") {
                do {
                    console = Input("\nWithdraw in multiples of 1.\n");
                    parser = int.TryParse(console, out amount);

                    //string divStr = divisor.ToString();
                    //
                    //parser = int.TryParse(divStr, out int numCheck);

                    request = true;
                } while (parser == false);

            } else if (console.ToLower() == "n" && validCard) {
                return amount;
            }

            drawerTotal = CheckDrawer(cashDrawer);

            if (validCard == false) {

                PaymentError(total, validCard, cashDrawer);
                total = 0;

                return total;
            }

            if ((request) && (drawerTotal < amount)) {
                PaymentError(total, validCard, cashDrawer);

                return total;
            }
            return amount;
        }

        static string[] MoneyRequest(string account_number, decimal amount) {
            Random rnd = new Random();
            //50% chance transaction passes or fails
            bool pass = rnd.Next(100) < 50;
            //50% chance that a failed transaction is declined
            bool declined = rnd.Next(100) < 50;

            if (pass) {
                return new string[] { account_number, amount.ToString() };
            } else {
                if (!declined) {
                    return new string[] { account_number, (amount / rnd.Next(2, 6)).ToString() };
                } else {
                    return new string[] { account_number, "declined" };
                }//end if
            }//end if
        }

        static decimal PaymentError(decimal total, bool validCard, Currency[] cashDrawer) {
            string console;
            int type;

            if (validCard == false) {
                Console.WriteLine("Invalid card");
            }
            DarkRedText("Unable to complete transaction.");
            DisplayTotal(total);
            Console.WriteLine();

            do {
                console = Input("Change payment method? (y/n)");
            } while (console.ToLower() != "y" && console.ToLower() != "n");

            if (console.ToLower() == "n") {
                Console.WriteLine("Transaction cancelled.");
                total = 0;
                paymentComplete = true;
                return total;

            } else if (console.ToLower() == "y") {
                return total;
            }
            return total;
        }

        #endregion

        #region post-payment

        static decimal DispenseChange(decimal CBamount, decimal changeDue, Currency[] cashDrawer, int[] intake) {
            decimal dispensed = 0.00m;
            decimal drawerTotal = CheckDrawer(cashDrawer);

            Console.WriteLine("\n      -------      ");

            if (CBamount > 0) {
                changeDue = CBamount;
            }

            if (changeDue > drawerTotal) {
                DarkRedText("\nInsufficient dispensable funds to complete this transaction.\nPlease pay another way.");
                paymentComplete = false;
                return changeDue;

            } else {

                for (int i = cashDrawer.Length - 1; i > -1; i--) {

                    if (changeDue > 0) {

                        while (changeDue >= cashDrawer[i].value && cashDrawer[i].drawerAmt > 0) {

                            cashDrawer[i].drawerAmt--;
                            changeDue = changeDue - cashDrawer[i].value;

                            dispensed = dispensed + cashDrawer[i].value;

                            Console.WriteLine($"Dispensed  {cashDrawer[i].value:C}");
                        }

                        if ((i == 0) && (cashDrawer[i].drawerAmt == 0) && (changeDue > 0)) {
                            DarkRedText("\nInsufficient available funds to complete this purchase.\nPlease pay another way.");
                            paymentComplete = false;
                            return changeDue;
                        }
                    }
                }
                GreenText($"\nDispensed total: {dispensed:C}");
                paymentComplete = true;

            }
            return dispensed;
        }

        static decimal CheckDrawer(Currency[] cashDrawer) {
            decimal drawerTotal = 0.00m;
            decimal drawerAmt = 0.00m;

            for (int drawer = 0; drawer < cashDrawer.Length; drawer++) {

                drawerAmt = cashDrawer[drawer].drawerAmt * cashDrawer[drawer].value;
                drawerTotal = drawerTotal + drawerAmt;
            }

            return drawerTotal;
        }

        static Currency[] RefreshDrawer(Currency[] cashDrawer, int[] intake) {

            for (int drawer = 0; drawer < cashDrawer.Length; drawer++) {
                cashDrawer[drawer].drawerAmt = cashDrawer[drawer].drawerAmt + intake[drawer];
            }
            return cashDrawer;
        }

        static string TransactionNumber() {
            Random rand = new Random();
            int[] nums = new int[10];
            string transactionNo = "";

            for (int i = 0; i < nums.Length; i++) {
                nums[i] = rand.Next(0, 10);
                transactionNo = transactionNo + nums[i];
            }
            return transactionNo;
        }

        static string GetDate() {
            string dateString;
            string[] months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
            DateTime dateTime = DateTime.Now;

            string[] currentDate = { dateTime.Month.ToString(), dateTime.Day.ToString(), dateTime.Year.ToString() };

            for (int month = 0; month < months.Length; month++) {
                if (dateTime.Month == month + 1) {
                    currentDate[0] = months[month];
                }
            }
            dateString = $"{currentDate[0]}-{currentDate[1]}-{currentDate[2]}";
            Console.WriteLine(dateString);
            return dateString;
        }

        static string GetTime() {
            string timeString;
            DateTime dateTime = DateTime.Now;

            string[] currentTime = { dateTime.Hour.ToString(), dateTime.Minute.ToString(), dateTime.Second.ToString() };
            timeString = $"{currentTime[0]}:{currentTime[1]}:{currentTime[2]}";

            return timeString;
        }

        static void LaunchLogger(string transactionNo, string dateString, string timeString, decimal cashIntakeTotal, string cardType, decimal cardAmount, decimal dispensed) {

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @"C:\Users\MCA\source\repos\Evaluations\TransactionLogger\bin\Debug\net6.0\TransactionLogger.exe";
            startInfo.Arguments = transactionNo + " " + dateString + " " + timeString + " " + cashIntakeTotal.ToString() + " " + cardType + " " + cardAmount.ToString() + " " + dispensed.ToString() + " ";
            Process.Start(startInfo);
        }

        static void DarkRedText(string text) {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(text);
            Console.ForegroundColor = ConsoleColor.White;
        }
        
        static void GreenText(string text) {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(text);
            Console.ForegroundColor = ConsoleColor.White;
        }

        #endregion

        static string Input(string prompt) {
            Console.Write(prompt);
            return Console.ReadLine();
        }
    }
