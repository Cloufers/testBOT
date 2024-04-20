using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Interface
{
    internal class Program
    {
        static void Main()
        {
            var client = new TelegramBotClient("6782678829:AAEod-hs_PM6yIBYta1eNapt9yIuSg_8tmQ");

            client.StartReceiving(Update, Error);
            Console.ReadLine();


        }


        async static Task Update(ITelegramBotClient botClient, Update update, CancellationToken token)
        {
            var message = update.Message;

            /* Commands */ 
            
            if (message.Text != null)
            {

                Console.WriteLine($"{message.Chat.FirstName} {message.Chat.LastName} | {message.Text}"); /* logs */

                if (message.Text.ToLower().Contains("ресурс") || message.Text.ToLower().Contains("jiafei"))
                {

                    await botClient.SendTextMessageAsync(message.Chat.Id, "Hello sweetheart");
                    return;

                } else if (message.Text.ToLower().StartsWith("/join"))
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Welcome to the cLuB! Type /join *team token* or type /create to create a brand new team");
                    return;   
                } 
                if (message.Text.ToLower().Contains("/create"))
                {
                    var userToken = await botClient.GetMeAsync();

                    
                    

                    await botClient.SendTextMessageAsync(message.Chat.Id, userToken.ToString());
                    return;
                }
                

                




            }
            
             /* Documents */

            if (message.Document != null)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Accepter wait for review");

                var fileId = update.Message.Document.FileId;
                var fileInfo = await botClient.GetFileAsync(fileId, token);
                var filePath = fileInfo.FilePath;
                var chatID = message.Chat.Id.ToString();

                string userFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), chatID);
                Directory.CreateDirectory(userFolderPath);

                string destinationFilePath = Path.Combine(userFolderPath, message.Document.FileName);

                await using Stream fileStream = System.IO.File.Create(destinationFilePath);
                await botClient.DownloadFileAsync(
                    filePath: filePath,
                    destination: fileStream,
                    cancellationToken: token);


                return;

            }
     
            {
                /* Dice */
                
                if (message.Dice != null)
                {


                    var diceValue = message.Dice.Value;
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"You threw dice with value:  {diceValue}");


                }                
            }  



        }

        private static Task Error(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}