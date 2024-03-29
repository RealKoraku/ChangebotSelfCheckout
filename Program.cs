﻿using System.Diagnostics;

namespace NewerKiosk;

public struct Currency {        //create Currency struct
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

        Currency[] cashDrawer = InitializeDrawer();      //fill drawer
        drawerTotal = CheckDrawer(cashDrawer);

        int[] intake = new int[cashDrawer.Length];

        while (true) {                                  //loop the program
            string dateString = GetDate();              //get current time and date
            string timeString = GetTime();

            decimal purchaseTotal = 0.00m;              //initialize variables
            decimal change = 0.00m;
            decimal CBamount = 0.00m;
            decimal dispensed = 0.00m;
            decimal cashIntakeTotal = 0.00m;
            decimal cardAmount = 0.00m;
            decimal acceptedAmt;
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

            DrawTitle();                                //Changebot ASCII title

            Console.WriteLine("Welcome to Changebot!\n");

            purchaseTotal = ScanItems();                //user scans items, total rounded to 2 decimal points
            purchaseTotal = decimal.Round(purchaseTotal, 2, MidpointRounding.AwayFromZero);
            DisplayTotal(purchaseTotal);

            while (paymentComplete == false) {          //loop while payment is not complete

                intake = new int[cashDrawer.Length];    //create cash intake array 

                type = SelectPaymentType();             //user selects payment type

                if (type == 1) {                        //cash payments
                    change = InsertCash(purchaseTotal, cashDrawer, intake);
                    cashIntakeTotal = cashIntake(intake, cashDrawer);
                    dispensed = DispenseChange(CBamount, change, cashDrawer, intake);
                    paymentComplete = true;

                } else if (type == 2) {                 //card payment
                    cardStr = InsertCard(purchaseTotal);
                    validCard = IsValidCard(cardStr);
                    cardType = CardType(cardStr);
                    GreenText(cardType);
                    CBamount = CashBack(purchaseTotal, cardStr, validCard, cashDrawer);
                    cardAmount = purchaseTotal + CBamount;
                                                        //'cardAmount' is full card charge
                    if (validCard) {

                        bankResponse = MoneyRequest(cardStr, cardAmount);
                        parser = decimal.TryParse(bankResponse[1], out acceptedAmt);
                        acceptedAmt = decimal.Round(acceptedAmt, 2, MidpointRounding.AwayFromZero);
                                                        //bank card response
                        if (bankResponse[1] == "declined") {
                            Console.WriteLine("Bank declined transaction.");

                            CBamount = 0;
                            cardAmount = 0;
                            purchaseTotal = PaymentError(purchaseTotal, validCard, cashDrawer);

                        } else if (acceptedAmt == cardAmount) {
                            GreenText($"Bank approved transaction.");
                            cardAmount = purchaseTotal + CBamount;
                            paymentComplete = true;

                        } else {                        //if payment partially accepted

                            Console.WriteLine($"Bank approved {acceptedAmt:C}.");

                            if (CBamount > 0) {         //cancel cashback if not fully approved
                                DarkRedText("\nCashback not approved.");
                                CBamount = 0;
                            }
                                                        //purchase still complete if approved over the item total
                            if (acceptedAmt > purchaseTotal) {
                                acceptedAmt = purchaseTotal;
                                cardAmount = purchaseTotal;
                                Console.WriteLine("Transaction complete.");
                                paymentComplete = true;

                            } else {                    //else purchase only partially complete

                                purchaseTotal = purchaseTotal - acceptedAmt;

                                cardAmount = acceptedAmt;
                                DisplayTotal(purchaseTotal);

                                do {                    //option to complete payment in cash
                                    console = Input("Finish payment in cash? (y/n)");
                                } while (console.ToLower() != "y" && console.ToLower() != "n");

                                if (console.ToLower() == "y") {
                                    change = InsertCash(purchaseTotal, cashDrawer, intake);
                                    cashIntakeTotal = cashIntake(intake, cashDrawer);
                                    paymentComplete = true;
                                } else {                
                                    Console.WriteLine("\nTransaction cancelled.");
                                    purchaseTotal = 0;
                                    cardAmount = 0;
                                    paymentComplete = true;
                                }
                            }
                        }
                    }
                    if (paymentComplete) {              //dispense change 
                        dispensed = DispenseChange(CBamount, change, cashDrawer, intake);

                    }
                }
            }
                                                        //redispense cash to drawer after purchase
            cashDrawer = RefreshDrawer(cashDrawer, intake);
            drawerTotal = CheckDrawer(cashDrawer);
            transactionNo = TransactionNumber();        //build transaction no
            Console.WriteLine("Thank you. Please take your change.\n");
                                                        //save to log
            LaunchLogger(transactionNo, dateString, timeString, cashIntakeTotal, cardType, cardAmount, dispensed);
                                                        //restart at while(true) loop
            Console.WriteLine("Press any key to begin next transaction.");
            Console.ReadKey();
            Console.Clear();
        }
    }//end main

    #region drawer
    static Currency[] InitializeDrawer() {
        Currency[] cashDrawer = new Currency[12];

        cashDrawer[0] = new("penny", 0.01m, 300);   //each currency has a type, value, and amount
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

                if ((console == "") && (item > 0)) {    //if 1 item scanned and blank, done
                    scanDone = true;
                }

                parser = decimal.TryParse(console, out itemPrice);

                if (itemPrice < 0) {                    //no negative prices
                    parser = false;
                }

            } while (parser == false && scanDone == false);

            total = total + itemPrice;                  //figure total
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
                console = Input($"\nPayment {pay + 1}  $");     //make payments, validate currency
                parser = decimal.TryParse(console, out payment);

                validCurrency = IsValidCurrency(payment, cashDrawer);

            } while (parser == false || validCurrency == false);

            total = total - payment;                            //subtract payment from total

            for (int drawer = 0; drawer < cashDrawer.Length; drawer++) {
                if (payment == cashDrawer[drawer].value) {
                    intake[drawer]++;                           //add each inserted cash to intake array
                }
            }

            if (total > 0.00m) {
                Console.WriteLine($"Remaining  {total:C}");     //show remaining total 
            }
        }
        decimal endTotal = total * -1;                          //flip to positive value

        GreenText($"\nChange     {endTotal:C}");                //show change due

        return endTotal;
    }

    static decimal cashIntake(int[] intake, Currency[] cashDrawer) {
        decimal cashIntakeTotal = 0.00m;

        for (int i = 0; i < intake.Length; i++) {                   //figure total cash inserted
            cashIntakeTotal = cashIntakeTotal + (intake[i] * cashDrawer[i].value);
        }
        return cashIntakeTotal;
    }

    static bool IsValidCurrency(decimal payment, Currency[] cashDrawer) {
        bool validCurrency = false;

        for (int drawer = 0; drawer < cashDrawer.Length; drawer++) {
            if (payment == cashDrawer[drawer].value) {
                validCurrency = true;                               //verify bills are valid/accepted in drawer
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

        if (cardStr[0] == '3') {                    //check first number of card for vendor
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
        int sumNum1;
        int sumNum2;
        string sumStr;
        int[] card = new int[cardStr.Length];
        bool mult2 = true;
        string productStr;
        bool isValidCard = false;
        int checkSum = 0;
        decimal finalSum = 0;
        decimal totalSum = 0;
        decimal endNum = 0;

        checkSum = (int)char.GetNumericValue(cardStr[cardStr.Length - 1]);


        for (int i = cardStr.Length - 2; i > -1; i--) {             //Luhn algorithm
            aNum = (int)char.GetNumericValue(cardStr[i]);

            if (mult2) {                                            //starting at 2nd to last digit,mult that digit by 2
                card[i] = aNum * 2;                                 
                mult2 = false;
            } else {
                card[i] = aNum * 1;                                 //next digit by 1, then 2, then 1 etc
                mult2 = true;
            }
            sumStr = card[i].ToString();                            //to string so we can add the 2 separate digits
            sumNum1 = (int)char.GetNumericValue(sumStr[0]);

            if (card[i] > 9) {                                      //if 2 digits
                sumNum2 = (int)char.GetNumericValue(sumStr[1]);
                finalSum = sumNum1 + sumNum2;
            } else {
                finalSum = sumNum1;
            }
            totalSum = totalSum + finalSum;
        }
        endNum = (10 - (totalSum % 10)) % 10;

        if (endNum == checkSum) {
            isValidCard = true;
        }

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

        drawerTotal = CheckDrawer(cashDrawer);

        do {
            console = Input("\nWould you like cash-back? (y/n)");
        } while (console.ToLower() != "n" && console.ToLower() != "y");

        if (console.ToLower() == "y") {
            do {
                console = Input("\nWithdraw in multiples of 1.\n");
                parser = int.TryParse(console, out amount);

                request = true;

                if (drawerTotal < amount) {    
                    DarkRedText("Insufficient funds to complete this request. Please try again.");
                }
            } while ((parser == false) || (drawerTotal < amount));

        } else if (console.ToLower() == "n" && validCard) {
            return amount;
        }

        if (validCard == false) {

            PaymentError(total, validCard, cashDrawer);
            total = 0;

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

        do {                            //change payment or cancel after error occured
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

        if (CBamount > 0) {     //cards will rceive cashback as change
            changeDue = CBamount;
        }

        if (changeDue > drawerTotal) {
            DarkRedText("\nInsufficient dispensable funds to complete this transaction.");
            paymentComplete = false;
            return changeDue;

        } else {

            for (int i = cashDrawer.Length - 1; i > -1; i--) {          //start at largest bill denomination in drawer

                if (changeDue > 0) {                                    //check if change is due

                    while (changeDue >= cashDrawer[i].value && cashDrawer[i].drawerAmt > 0) {
                                                                        //dispense highest possible bill amount / if bill is in drawer
                        cashDrawer[i].drawerAmt--;                      //deduct that bill from drawer
                        changeDue = changeDue - cashDrawer[i].value;    //deduct from change due

                        dispensed = dispensed + cashDrawer[i].value;    //keep track of dispensed total

                        Console.WriteLine($"Dispensed  {cashDrawer[i].value:C}");
                    }

                    if ((i == 0) && (cashDrawer[i].drawerAmt == 0) && (changeDue > 0)) {
                        DarkRedText("\nInsufficient dispensable funds to complete this purchase.\n");
                        paymentComplete = false;
                        return changeDue;
                    }
                }
            }
            GreenText($"\nDispensed total: {dispensed:C}");             //notify dispensed total
            paymentComplete = true;

        }
        return dispensed;
    }

    static decimal CheckDrawer(Currency[] cashDrawer) {
        decimal drawerTotal = 0.00m;
        decimal drawerAmt = 0.00m;

        for (int drawer = 0; drawer < cashDrawer.Length; drawer++) {    //find drawer total by multiplying each bill value and amount

            drawerAmt = cashDrawer[drawer].drawerAmt * cashDrawer[drawer].value;
            drawerTotal = drawerTotal + drawerAmt;
        }

        return drawerTotal;
    }

    static Currency[] RefreshDrawer(Currency[] cashDrawer, int[] intake) {

        for (int drawer = 0; drawer < cashDrawer.Length; drawer++) {    //add cash intake to drawer after purchase
            cashDrawer[drawer].drawerAmt = cashDrawer[drawer].drawerAmt + intake[drawer];
        }
        return cashDrawer;
    }

    static string TransactionNumber() {
        Random rand = new Random();
        int[] nums = new int[10];
        string transactionNo = "";

        for (int i = 0; i < nums.Length; i++) {                         //create transaction string of 10 random ints
            nums[i] = rand.Next(0, 10);
            transactionNo = transactionNo + nums[i];
        }
        return transactionNo;
    }

    static string GetDate() {                                           //format the date
        string dateString;
        string[] months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        DateTime dateTime = DateTime.Now;
                                                                        
        string[] currentDate = { dateTime.Month.ToString(), dateTime.Day.ToString(), dateTime.Year.ToString() };

        for (int month = 0; month < months.Length; month++) {           //figure month abbreviation
            if (dateTime.Month == month + 1) {
                currentDate[0] = months[month];
            }
        }                                                               //build formatted date string
        dateString = $"{currentDate[0]}-{currentDate[1]}-{currentDate[2]}";
        Console.WriteLine(dateString);
        return dateString;
    }

    static string GetTime() {
        string timeString;
        DateTime dateTime = DateTime.Now;
                                                                        //build formatted time string
        string[] currentTime = { dateTime.Hour.ToString(), dateTime.Minute.ToString(), dateTime.Second.ToString() };
        timeString = $"{currentTime[0]}:{currentTime[1]}:{currentTime[2]}";

        return timeString;
    }

    static void LaunchLogger(string transactionNo, string dateString, string timeString, decimal cashIntakeTotal, string cardType, decimal cardAmount, decimal dispensed) {

        string workingDirectory = Environment.CurrentDirectory;
        string path = workingDirectory + "\\TransactionLogger.exe";

        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = path;
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

    static void BlueText(string text) {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine(text);
        Console.ForegroundColor = ConsoleColor.White;
    }

    static void DrawTitle() {
        BlueText("   ____");
        BlueText("  /     /    _    _    ___  __   /        _/_");
        BlueText(" /     /__  __\\  / \\  /  / /_/  /__  ___  /");
        BlueText("/____ /  / /__/ /  / /__/ /___ /__/ /__/ /");
        BlueText("                    ___/  NoHomoSapiens Corp.\n");
    }

    #endregion

    static string Input(string prompt) {
        Console.Write(prompt);
        return Console.ReadLine();
    }
}