using System.Globalization;
using System.Resources;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.ConsoleUI;

public class ConsoleApplication
{
    private readonly ResourceManager _resources;
    private readonly AccountService _accountService;
    private readonly TransferKindService _transferKindService;
    private readonly TransferService _transferService;

    public ConsoleApplication(
        ResourceManager resources,
        AccountService accountService,
        TransferKindService transferKindService,
        TransferService transferService)
    {
        _resources = resources ?? throw new ArgumentNullException(nameof(resources));
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        _transferKindService = transferKindService ?? throw new ArgumentNullException(nameof(transferKindService));
        _transferService = transferService ?? throw new ArgumentNullException(nameof(transferService));
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var isRunning = true;

        while (isRunning && !cancellationToken.IsCancellationRequested)
        {
            DisplayMenu();

            Console.Write($"{GetText("MenuChooseOption")} ");
            var selectedOption = Console.ReadLine();

            if (selectedOption is null)
            {
                return;
            }

            try
            {
                switch (selectedOption.Trim())
                {
                    case "1":
                        await HandleCreateAccountAsync(cancellationToken);
                        break;
                    case "2":
                        await HandleListAccountsAsync(cancellationToken);
                        break;
                    case "3":
                        await HandleCreateTransferKindAsync(cancellationToken);
                        break;
                    case "4":
                        await HandleListTransferKindsAsync(cancellationToken);
                        break;
                    case "5":
                        await HandleCreateTransferAsync(cancellationToken);
                        break;
                    case "6":
                        await HandleViewAccountAsync(cancellationToken);
                        break;
                    case "7":
                        await HandleCompleteTransferAsync(cancellationToken);
                        break;
                    case "0":
                        isRunning = false;
                        break;
                    default:
                        Console.WriteLine(GetText("InvalidOption"));
                        break;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (DbUpdateException)
            {
                Console.WriteLine(GetText("DatabaseError"));
            }
            catch (ArgumentException)
            {
                Console.WriteLine(GetText("InvalidData"));
            }
        }
    }

    private void DisplayMenu()
    {
        Console.WriteLine();
        Console.WriteLine(GetText("MenuTitle"));
        Console.WriteLine(GetText("MenuCreateAccount"));
        Console.WriteLine(GetText("MenuListAccounts"));
        Console.WriteLine(GetText("MenuCreateTransferKind"));
        Console.WriteLine(GetText("MenuListTransferKinds"));
        Console.WriteLine(GetText("MenuCreateTransfer"));
        Console.WriteLine(GetText("MenuViewAccount"));
        Console.WriteLine(GetText("MenuCompleteTransfer"));
        Console.WriteLine(GetText("MenuExit"));
    }

    private async Task HandleCreateAccountAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine();
        Console.WriteLine(GetText("CreateAccountTitle"));

        var accountName = ReadRequiredText(
            "AccountNamePrompt",
            "InvalidAccountName",
            cancellationToken);

        if (accountName is null)
        {
            return;
        }

        AccountType? accountType = null;

        while (accountType is null && !cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine(GetText("AccountTypePrompt"));
            Console.WriteLine(GetText("AccountTypeChecking"));
            Console.WriteLine(GetText("AccountTypeSavings"));
            Console.WriteLine(GetText("AccountTypeCash"));
            Console.WriteLine(GetText("AccountTypeCreditCard"));
            Console.WriteLine(GetText("AccountTypeInvestment"));
            Console.WriteLine(GetText("AccountTypeOther"));

            var input = Console.ReadLine();
            if (input is null)
            {
                return;
            }

            accountType = input.Trim() switch
            {
                "1" => AccountType.CheckingAccount,
                "2" => AccountType.SavingsAccount,
                "3" => AccountType.Cash,
                "4" => AccountType.CreditCard,
                "5" => AccountType.InvestmentPortfolio,
                "6" => AccountType.Other,
                _ => null
            };

            if (accountType is null)
            {
                Console.WriteLine(GetText("InvalidAccountType"));
            }
        }

        decimal? initialBalance = null;

        while (initialBalance is null && !cancellationToken.IsCancellationRequested)
        {
            Console.Write($"{GetText("InitialBalancePrompt")} ");
            var input = Console.ReadLine();
            if (input is null)
            {
                return;
            }

            if (TryParseMoney(input, out var parsedBalance))
            {
                initialBalance = parsedBalance;
            }
            else
            {
                Console.WriteLine(GetText("InvalidInitialBalance"));
            }
        }

        if (accountType is null || initialBalance is null || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        var account = await _accountService.CreateAccountAsync(
            accountName,
            accountType.Value,
            initialBalance.Value,
            cancellationToken);

        Console.WriteLine(FormatText("AccountCreatedSuccessfully", account.Name, account.Id));
    }

    private async Task HandleListAccountsAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine();
        Console.WriteLine(GetText("AccountsTitle"));

        var accounts = await _accountService.GetAccountsAsync(cancellationToken);
        DisplayAccounts(accounts);
    }

    private async Task HandleCreateTransferKindAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine();
        Console.WriteLine(GetText("CreateTransferKindTitle"));

        var name = ReadRequiredText(
            "TransferKindNamePrompt",
            "InvalidTransferKindName",
            cancellationToken);

        if (name is null)
        {
            return;
        }

        TransferKindMode? mode = null;

        while (mode is null && !cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine(GetText("TransferKindModePrompt"));
            Console.WriteLine(GetText("TransferKindModeIncomeOption"));
            Console.WriteLine(GetText("TransferKindModeExpenseOption"));
            Console.WriteLine(GetText("TransferKindModeBothOption"));

            var input = Console.ReadLine();
            if (input is null)
            {
                return;
            }

            mode = input.Trim() switch
            {
                "1" => TransferKindMode.Income,
                "2" => TransferKindMode.Expense,
                "3" => TransferKindMode.IncomeAndExpense,
                _ => null
            };

            if (mode is null)
            {
                Console.WriteLine(GetText("InvalidTransferKindMode"));
            }
        }

        if (mode is null || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        var kind = await _transferKindService.CreateTransferKindAsync(
            name,
            mode.Value,
            cancellationToken);

        Console.WriteLine(FormatText("TransferKindCreatedSuccessfully", kind.Name, kind.Id));
    }

    private async Task HandleListTransferKindsAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine();
        Console.WriteLine(GetText("TransferKindsTitle"));

        var kinds = await _transferKindService.GetTransferKindsAsync(cancellationToken);
        DisplayTransferKinds(kinds);
    }

    private async Task HandleCreateTransferAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine();
        Console.WriteLine(GetText("CreateTransferTitle"));

        var accounts = await _accountService.GetAccountsAsync(cancellationToken);
        if (accounts.Count == 0)
        {
            Console.WriteLine(GetText("NoAccounts"));
            return;
        }

        DisplayAccounts(accounts);
        var account = SelectAccount(accounts, cancellationToken);
        if (account is null)
        {
            return;
        }

        var transferMode = ReadTransferMode(cancellationToken);
        if (transferMode is null)
        {
            return;
        }

        var kinds = await _transferKindService.GetTransferKindsAsync(cancellationToken);
        var compatibleKinds = kinds
            .Where(kind => IsCompatible(kind.TransferKindMode, transferMode.Value))
            .ToList();

        if (compatibleKinds.Count == 0)
        {
            Console.WriteLine(GetText("NoCompatibleTransferKinds"));
            return;
        }

        DisplayTransferKinds(compatibleKinds);
        var transferKind = SelectTransferKind(compatibleKinds, cancellationToken);
        if (transferKind is null)
        {
            return;
        }

        decimal? amount = null;

        while (amount is null && !cancellationToken.IsCancellationRequested)
        {
            Console.Write($"{GetText("TransferAmountPrompt")} ");
            var input = Console.ReadLine();
            if (input is null)
            {
                return;
            }

            if (TryParseMoney(input, out var parsedAmount) && parsedAmount > 0)
            {
                amount = parsedAmount;
            }
            else
            {
                Console.WriteLine(GetText("InvalidTransferAmount"));
            }
        }

        Console.Write($"{GetText("TransferDescriptionPrompt")} ");
        var descriptionInput = Console.ReadLine();
        if (descriptionInput is null)
        {
            return;
        }

        var description = string.IsNullOrWhiteSpace(descriptionInput)
            ? null
            : descriptionInput.Trim();

        DateTime? effectiveAt = null;

        while (effectiveAt is null && !cancellationToken.IsCancellationRequested)
        {
            Console.Write($"{FormatText("TransferDatePrompt", DateTime.Today.ToString("d"))} ");
            var input = Console.ReadLine();
            if (input is null)
            {
                return;
            }

            if (DateTime.TryParse(
                    input,
                    CultureInfo.CurrentCulture,
                    DateTimeStyles.AllowWhiteSpaces,
                    out var parsedDate) &&
                parsedDate.Year is >= 2020 and <= 2100)
            {
                effectiveAt = parsedDate.Date;
            }
            else
            {
                Console.WriteLine(GetText("InvalidTransferDate"));
            }
        }

        bool? isCompleted = null;

        while (isCompleted is null && !cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine(GetText("TransferStatusPrompt"));
            Console.WriteLine(GetText("TransferStatusCompletedOption"));
            Console.WriteLine(GetText("TransferStatusPendingOption"));

            var input = Console.ReadLine();
            if (input is null)
            {
                return;
            }

            isCompleted = input.Trim() switch
            {
                "1" when effectiveAt <= DateTime.Today => true,
                "2" => false,
                _ => null
            };

            if (isCompleted is null)
            {
                Console.WriteLine(effectiveAt > DateTime.Today && input.Trim() == "1"
                    ? GetText("FutureTransferCannotBeCompleted")
                    : GetText("InvalidTransferStatus"));
            }
        }

        if (amount is null || effectiveAt is null || isCompleted is null ||
            cancellationToken.IsCancellationRequested)
        {
            return;
        }

        var transfer = await _transferService.CreateTransferAsync(
            amount.Value,
            description,
            effectiveAt.Value,
            transferMode.Value,
            isCompleted.Value,
            account,
            transferKind,
            cancellationToken);

        Console.WriteLine(FormatText("TransferCreatedSuccessfully", transfer.Id));
    }

    private async Task HandleViewAccountAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine();
        Console.WriteLine(GetText("ViewAccountTitle"));

        var accounts = await _accountService.GetAccountsAsync(cancellationToken);
        if (accounts.Count == 0)
        {
            Console.WriteLine(GetText("NoAccounts"));
            return;
        }

        DisplayAccounts(accounts);
        var selectedAccount = SelectAccount(accounts, cancellationToken);
        if (selectedAccount is null)
        {
            return;
        }

        var account = await _accountService.GetAccountByIdAsync(
            selectedAccount.Id,
            cancellationToken);

        if (account is null)
        {
            Console.WriteLine(GetText("AccountNotFound"));
            return;
        }

        Console.WriteLine();
        Console.WriteLine(FormatText("AccountDetailsName", account.Name));
        Console.WriteLine(FormatText("AccountDetailsType", GetAccountTypeText(account.AccountType)));
        Console.WriteLine(FormatText("AccountDetailsInitialBalance", account.InitialBalance));
        Console.WriteLine(FormatText("AccountDetailsCurrentBalance", account.GetAccountBalance()));
        Console.WriteLine(FormatText("AccountDetailsTotalExpenses", account.GetTotalExpense()));
        Console.WriteLine(GetText("AccountTransfersTitle"));

        var transfers = account.GetTransfersFromNewest();
        if (transfers.Count == 0)
        {
            Console.WriteLine(GetText("NoTransfers"));
            return;
        }

        foreach (var transfer in transfers)
        {
            Console.WriteLine(FormatText(
                "TransferListItem",
                transfer.Id,
                transfer.EffectiveAt,
                GetTransferModeText(transfer.TransferMode),
                transfer.TransferKind.Name,
                transfer.Amount,
                GetTransferStatusText(transfer.Status),
                transfer.Description ?? GetText("NoDescription")));
        }
    }

    private async Task HandleCompleteTransferAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine();
        Console.WriteLine(GetText("CompleteTransferTitle"));

        var transfers = await _transferService.GetCompletableTransfersAsync(
            DateTime.Today,
            cancellationToken);

        if (transfers.Count == 0)
        {
            Console.WriteLine(GetText("NoCompletableTransfers"));
            return;
        }

        foreach (var transfer in transfers)
        {
            Console.WriteLine(FormatText(
                "CompletableTransferListItem",
                transfer.Id,
                transfer.EffectiveAt,
                transfer.Account.Name,
                GetTransferModeText(transfer.TransferMode),
                transfer.TransferKind.Name,
                transfer.Amount));
        }

        Transfer? selectedTransfer = null;

        while (selectedTransfer is null && !cancellationToken.IsCancellationRequested)
        {
            Console.Write($"{GetText("TransferIdPrompt")} ");
            var input = Console.ReadLine();
            if (input is null)
            {
                return;
            }

            if (int.TryParse(input, out var id))
            {
                selectedTransfer = transfers.SingleOrDefault(
                    transfer => transfer.Id == id);
            }

            if (selectedTransfer is null)
            {
                Console.WriteLine(GetText("InvalidTransferId"));
            }
        }

        if (selectedTransfer is null || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        var completedTransfer = await _transferService.MarkTransferAsCompletedAsync(
            selectedTransfer.Id,
            cancellationToken);

        Console.WriteLine(completedTransfer is null
            ? GetText("TransferNotFound")
            : FormatText("TransferCompletedSuccessfully", completedTransfer.Id));
    }

    private string? ReadRequiredText(
        string promptKey,
        string invalidMessageKey,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            Console.Write($"{GetText(promptKey)} ");
            var input = Console.ReadLine();

            if (input is null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(input))
            {
                return input.Trim();
            }

            Console.WriteLine(GetText(invalidMessageKey));
        }

        return null;
    }

    private TransferMode? ReadTransferMode(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine(GetText("TransferModePrompt"));
            Console.WriteLine(GetText("TransferModeIncomeOption"));
            Console.WriteLine(GetText("TransferModeExpenseOption"));

            var input = Console.ReadLine();
            if (input is null)
            {
                return null;
            }

            var mode = input.Trim() switch
            {
                "1" => TransferMode.Income,
                "2" => TransferMode.Expense,
                _ => (TransferMode?)null
            };

            if (mode is not null)
            {
                return mode;
            }

            Console.WriteLine(GetText("InvalidTransferMode"));
        }

        return null;
    }

    private Account? SelectAccount(
        IReadOnlyCollection<Account> accounts,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            Console.Write($"{GetText("AccountIdPrompt")} ");
            var input = Console.ReadLine();
            if (input is null)
            {
                return null;
            }

            if (int.TryParse(input, out var id))
            {
                var account = accounts.SingleOrDefault(candidate => candidate.Id == id);
                if (account is not null)
                {
                    return account;
                }
            }

            Console.WriteLine(GetText("InvalidAccountId"));
        }

        return null;
    }

    private TransferKind? SelectTransferKind(
        IReadOnlyCollection<TransferKind> kinds,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            Console.Write($"{GetText("TransferKindIdPrompt")} ");
            var input = Console.ReadLine();
            if (input is null)
            {
                return null;
            }

            if (int.TryParse(input, out var id))
            {
                var kind = kinds.SingleOrDefault(candidate => candidate.Id == id);
                if (kind is not null)
                {
                    return kind;
                }
            }

            Console.WriteLine(GetText("InvalidTransferKindId"));
        }

        return null;
    }

    private void DisplayAccounts(IReadOnlyCollection<Account> accounts)
    {
        if (accounts.Count == 0)
        {
            Console.WriteLine(GetText("NoAccounts"));
            return;
        }

        foreach (var account in accounts)
        {
            Console.WriteLine(FormatText(
                "AccountListItem",
                account.Id,
                account.Name,
                GetAccountTypeText(account.AccountType),
                account.InitialBalance,
                account.GetAccountBalance()));
        }
    }

    private void DisplayTransferKinds(IReadOnlyCollection<TransferKind> kinds)
    {
        if (kinds.Count == 0)
        {
            Console.WriteLine(GetText("NoTransferKinds"));
            return;
        }

        foreach (var kind in kinds)
        {
            Console.WriteLine(FormatText(
                "TransferKindListItem",
                kind.Id,
                kind.Name,
                GetTransferKindModeText(kind.TransferKindMode)));
        }
    }

    private string GetAccountTypeText(AccountType accountType)
    {
        var key = accountType switch
        {
            AccountType.CheckingAccount => "AccountTypeCheckingName",
            AccountType.SavingsAccount => "AccountTypeSavingsName",
            AccountType.Cash => "AccountTypeCashName",
            AccountType.CreditCard => "AccountTypeCreditCardName",
            AccountType.InvestmentPortfolio => "AccountTypeInvestmentName",
            AccountType.Other => "AccountTypeOtherName",
            _ => throw new ArgumentOutOfRangeException(nameof(accountType))
        };

        return GetText(key);
    }

    private string GetTransferKindModeText(TransferKindMode mode)
    {
        var key = mode switch
        {
            TransferKindMode.Income => "TransferKindModeIncomeName",
            TransferKindMode.Expense => "TransferKindModeExpenseName",
            TransferKindMode.IncomeAndExpense => "TransferKindModeBothName",
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };

        return GetText(key);
    }

    private string GetTransferModeText(TransferMode mode)
    {
        return GetText(mode == TransferMode.Income
            ? "TransferModeIncomeName"
            : "TransferModeExpenseName");
    }

    private string GetTransferStatusText(TransferStatus status)
    {
        var key = status switch
        {
            TransferStatus.Completed => "TransferStatusCompletedName",
            TransferStatus.Scheduled => "TransferStatusScheduledName",
            TransferStatus.Overdue => "TransferStatusOverdueName",
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };

        return GetText(key);
    }

    private static bool IsCompatible(TransferKindMode kindMode, TransferMode transferMode)
    {
        return kindMode == TransferKindMode.IncomeAndExpense ||
               kindMode == TransferKindMode.Income && transferMode == TransferMode.Income ||
               kindMode == TransferKindMode.Expense && transferMode == TransferMode.Expense;
    }

    private static bool TryParseMoney(string input, out decimal value)
    {
        return decimal.TryParse(
                   input,
                   NumberStyles.Currency,
                   CultureInfo.CurrentCulture,
                   out value) &&
               decimal.Round(value, 2) == value;
    }

    private string FormatText(string key, params object[] arguments)
    {
        return string.Format(CultureInfo.CurrentCulture, GetText(key), arguments);
    }

    private string GetText(string key)
    {
        return _resources.GetString(key)
               ?? throw new MissingManifestResourceException(
                   $"Resource key '{key}' was not found.");
    }
}
