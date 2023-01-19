using System.Diagnostics;

namespace NewerKiosk {
        public struct Currency {
            public string type;
            public decimal value;
            public int drawerAmt;
        }
        internal class Program {
            public static bool paymentComplete;
            static void Main(string[] args) {
                decimal drawerTotal = 0.00m;
                decimal total = 0.00m;
                decimal change = 0.00m;
                decimal amount = 0.00m;
                decimal dispensed = 0.00m;
                int type;
                string cardStr;
                string cardType = "";
                bool validCard;
                paymentComplete = false;

                Console.WriteLine("Welcome to Changebot! ©2023 NoHomoSapiens\n");

                Currency[] cashDrawer = InitializeDrawer();
                drawerTotal = CheckDrawer(cashDrawer);

                int[] intake = new int[cashDrawer.Length];

                Console.WriteLine(drawerTotal);

                total = ScanItems();
                DisplayTotal(total);

                while (paymentComplete == false) {
                    type = SelectPaymentType();

                    if (type == 1) {

                        change = InsertCash(total, cashDrawer, intake);
                        dispensed = DispenseChange(change, cashDrawer, intake);
                        paymentComplete = true;
                    } else if (type == 2) {

                        cardStr = InsertCard(total);
                        validCard = IsValidCard(cardStr);
                        cardType = CardType(cardStr);
                        amount = CashBack(total, cardStr, validCard, cashDrawer);

                        if (paymentComplete) {
                            DispenseChange(amount, cashDrawer, intake);
                        }
                    }
                }

                cashDrawer = RefreshDrawer(cashDrawer, intake);
                drawerTotal = CheckDrawer(cashDrawer);
                Console.WriteLine($"(New drawer) = {drawerTotal}");

                LaunchLogger(intake, cardType, total, dispensed);

            }//end main

            #region drawer
            static Currency[] InitializeDrawer() {
                Currency[] cashDrawer = new Currency[12];

                cashDrawer[0] = CreateCurrency("penny", 0.01m, 200);
                cashDrawer[1] = CreateCurrency("nickel", 0.05m, 200);
                cashDrawer[2] = CreateCurrency("dime", 0.10m, 200);
                cashDrawer[3] = CreateCurrency("quarter", 0.25m, 200);
                cashDrawer[4] = CreateCurrency("half-dollar", 0.50m, 0);
                cashDrawer[5] = CreateCurrency("one", 1.00m, 200);
                cashDrawer[6] = CreateCurrency("two", 2.00m, 0);
                cashDrawer[7] = CreateCurrency("five", 5.00m, 100);
                cashDrawer[8] = CreateCurrency("ten", 10.00m, 50);
                cashDrawer[9] = CreateCurrency("twenty", 20.00m, 40);
                cashDrawer[10] = CreateCurrency("fifty", 50.00m, 40);
                cashDrawer[11] = CreateCurrency("hundred", 100.00m, 20);

                return cashDrawer;
            }

            static Currency CreateCurrency(string type, decimal value, int amt) {
                Currency denom = new Currency();

                denom.type = type;
                denom.value = value;
                denom.drawerAmt = amt;

                return denom;
            }
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
                Console.WriteLine($"\nTotal      {total:C}");
            }

            static int SelectPaymentType() {
                string console;
                bool parser;
                int type;

                do {
                    console = Input("\nSelect payment type:\nCash (1)\nCard (2)");
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
                        console = Input($"Payment {pay + 1}  $");
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

                Console.WriteLine($"\nChange     {endTotal:C}");

                return endTotal;
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
                string cardType = "";

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
                decimal amount = 0;
                decimal withdrawal;
                decimal drawerTotal;
                string account_number = cardStr;
                string[] bankInfo;
                int[] intake = { 0 };

                do {
                    console = Input("\nWould you like cash-back? (y/n)");
                } while (console != "n" && console != "y");

                if (console == "y") {
                    do {
                        console = Input("\nWithdraw in intervals of 10.\n(min $10 / max $200)");
                        parser = decimal.TryParse(console, out amount);

                        decimal divisor = amount / 10;
                        string divStr = divisor.ToString();

                        parser = int.TryParse(divStr, out int numCheck);

                        request = true;
                    } while (parser == false);
                }

                drawerTotal = CheckDrawer(cashDrawer);

                if (validCard == false) {

                    total = PaymentError(total, cashDrawer);
                    return total;
                }

                if (request) {
                    if (drawerTotal > amount) {
                        bankInfo = MoneyRequest(account_number, amount);

                        parser2 = decimal.TryParse(bankInfo[1], out withdrawal);

                        if (parser2 == false) {
                            Console.WriteLine("\nBank declined cashback.");
                            amount = 0;
                        } else if (withdrawal == amount) {
                            Console.WriteLine("\nCashback successful.");
                        } else {
                            Console.WriteLine($"\nPartial cashback successful: {withdrawal:C}");
                            amount = withdrawal;
                        }
                    }
                    paymentComplete = true;
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

            static decimal PaymentError(decimal total, Currency[] cashDrawer) {
                string console;
                int type;

                Console.WriteLine("Invalid card; unable to complete transaction.");
                DisplayTotal(total);
                Console.WriteLine();

                do {
                    console = Input("Change payment method? (y/n)");
                } while (console != "y" && console != "n");

                if (console == "n") {
                    Console.WriteLine("Transaction cancelled.");
                    total = 0;
                    paymentComplete = true;
                    return total;

                } else if (console == "y") {
                    return total;
                }
                return total;
            }

            #endregion

            #region post-payment

            static decimal DispenseChange(decimal changeDue, Currency[] cashDrawer, int[] intake) {
                decimal dispensed = 0.00m;
                decimal drawerTotal = CheckDrawer(cashDrawer);

                Console.WriteLine("\n      -------      ");

                if (changeDue > drawerTotal) {
                    Console.WriteLine("\nInsufficient dispensable funds to complete this transaction.\nPlease pay another way.");
                    InsertCard(changeDue);
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
                                Console.WriteLine("\nInsufficient available funds to complete this purchase.\nPlease pay another way.");
                                InsertCard(changeDue);
                            }
                        }
                    }
                    Console.WriteLine($"\nDispensed total: {dispensed}");

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

            static void LaunchLogger(int[] intake, string cardType, decimal total, decimal dispensed) {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "LogPackage.exe";
                startInfo.Arguments = $"{cardType}";
                Process.Start(startInfo);

            }

            #endregion

            static string Input(string prompt) {
                Console.Write(prompt);
                return Console.ReadLine();
            }
        }
    }