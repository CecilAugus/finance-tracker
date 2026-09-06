using System.Resources;
using System.Globalization;

namespace FinanceTracker.ConsoleUI;

public class ConsoleApplication
{
    private readonly ResourceManager _resources;
    private readonly AccountService _accountService;

    public ConsoleApplication(
        ResourceManager resources,
        AccountService accountService)
    {
        _resources = resources ?? throw new ArgumentNullException(nameof(resources));
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
    }

    private async Task HandleCreateAccountAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine();
        Console.WriteLine(GetText("CreateAccountTitle"));
        
        var accountName = "";

        while (string.IsNullOrWhiteSpace(accountName)
               && !cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine($"{GetText("AccountNamePrompt")} ");
            var input = Console.ReadLine();

            if (input is null)
            {
                return; 
            }

            accountName = input.Trim();

            if (string.IsNullOrWhiteSpace(accountName))
            {
                Console.WriteLine(GetText("InvalidAccountName"));
            }
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return; 
        }
    }

    private string GetText(string key)
    {
        return _resources.GetString(key)
               ?? throw new MissingManifestResourceException(
                   $"Resource key '{key}' was not found.");
    }


    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var isRunning = true;

        while (isRunning && !cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine();
            Console.WriteLine(GetText("MenuTitle"));
            Console.WriteLine(GetText("MenuCreateAccount"));
            Console.WriteLine(GetText("MenuListAccounts"));
            Console.WriteLine(GetText("MenuCreateTransferKind"));
            Console.WriteLine(GetText("MenuListTransferKinds"));
            Console.WriteLine(GetText("MenuCreateTransfer"));
            Console.WriteLine(GetText("MenuViewAccount"));
            Console.WriteLine(GetText("MenuExit"));

            Console.Write(GetText("MenuChooseOption") + " ");
            var selectedOption = Console.ReadLine();

            switch (selectedOption)
            {
                case "1":
                    await HandleCreateAccountAsync(cancellationToken);
                    break;

                case "2":

                case "3":

                case "4":

                case "5":

                case "6":
                    Console.WriteLine(GetText("NotImplemented"));
                    break;

                case "0":
                    isRunning = false;
                    break;

                default:
                    Console.WriteLine(GetText("InvalidOption"));
                    break;
            }
        }
    }
}