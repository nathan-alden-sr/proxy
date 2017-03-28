using System;
using System.Text;

namespace NathanAlden.Proxy.Services.CredentialService
{
    public class CredentialService : ICredentialService
    {
        private string _clearTextPassword;
        private string _username;

        public (GetCredentialsResult result, string username, string clearTextPassword) GetCredentials(string username = null)
        {
            if (_username == null || _clearTextPassword == null)
            {
                Console.Write("Username: ");

                if (!string.IsNullOrEmpty(username))
                {
                    Console.WriteLine(username);
                }
                else
                {
                    username = Console.ReadLine();
                }

                if (string.IsNullOrEmpty(username))
                {
                    return (GetCredentialsResult.Canceled, null, null);
                }

                Console.Write("Password: ");

                var stringBuilder = new StringBuilder();

                while (true)
                {
                    ConsoleKeyInfo consoleKeyInfo = Console.ReadKey(true);

                    switch (consoleKeyInfo.Key)
                    {
                        case ConsoleKey.Enter:
                            Console.WriteLine();
                            goto done;
                        case ConsoleKey.Escape:
                            return (GetCredentialsResult.Canceled, null, null);
                        case ConsoleKey.Backspace:
                            if (stringBuilder.Length > 0)
                            {
                                Console.Write("\b\0\b");
                                stringBuilder.Length--;
                            }
                            break;
                        default:
                            if (!char.IsControl(consoleKeyInfo.KeyChar))
                            {
                                Console.Write('*');
                                stringBuilder.Append(consoleKeyInfo.KeyChar);
                            }
                            break;
                    }
                }

                done:
                _username = username;
                _clearTextPassword = stringBuilder.ToString();
            }

            return (GetCredentialsResult.Success, _username, _clearTextPassword);
        }
    }
}