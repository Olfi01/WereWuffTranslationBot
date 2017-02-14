﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.SqlServer;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Types;
using WereWuffTranslationBot.CustomClasses;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using WereWuffTranslationBot.CustomKeyboards;
using System.Net;
using System.IO;

namespace WereWuffTranslationBot
{
    class Program
    {
        #region Constants
        private const string botApiToken = "355243674:AAEs0O6xDbs3k1fJCb2s2MAn0_7ZID7Rp7s";
        private const string botUsername = "@werewufftransbot";
        private const string startMessage = "You've just sucessfully started the WereWuff Tranlation Bot!\n" +
            "It's still under development though xD";
        private const int flomsId = 267376056;
        private const string closedlistPhpUrl = "http://127.0.0.1/getClosedlist.php";
        private const string underdevPhpUrl = "http://127.0.0.1/getUnderdev.php";
        private const string addClosedlistPhpUrl = "http://127.0.0.1/addClosedlist.php";
        private const string channelUsername = "@werewufftranstestchannel";
        private const int messageIdClosedlist = 3;
        private const int messageIdUnderdev = 4;
#if DEBUG
        private const string sqlString = "user id=test;password=test123;server=localhost:3306;" + 
            "Trusted_Connection=true;database=mysql; connection timeout=10";
#endif
#endregion
        #region Variables
        private static bool running = true;
        private static TelegramBotClient client = new TelegramBotClient(botApiToken);
        private static User me;
        private static Dictionary<long, string> waitingFor = new Dictionary<long, string>();
        #endregion
        #region Main Method
        static void Main(string[] args)
        {
            #region Initializing stuff
            try
            {
                Task<User> ut = client.GetMeAsync();
                Console.WriteLine("Getting information about this bot...");
                ut.Wait();
                me = ut.Result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString() + e.Message + e.StackTrace);
            }
            client.OnUpdate += Client_OnUpdate;
            #endregion
            while (running)
            {
                Console.WriteLine("Enter command");
                string input = Console.ReadLine();
                switch (input)
                {
                    case "startbot":
                        if (!client.IsReceiving)
                        {
                            client.StartReceiving();
                            Console.WriteLine("Bot started");
                        }
                        else
                        {
                            Console.WriteLine("Bot already running");
                        }
                        break;
                    case "stopbot":
                        if (client.IsReceiving)
                        {
                            client.StopReceiving();
                            Console.WriteLine("Bot stopped");
                        }
                        else
                        {
                            Console.WriteLine("Bot not running");
                        }
                        break;
                    case "exit":
                        if (client.IsReceiving)
                        {
                            client.StopReceiving();
                            Console.WriteLine("Bot stopping");
                        }
                        running = false;
                        Console.WriteLine("Exiting...");
                        break;
                    default:
                        Console.WriteLine("Command not found");
                        break;
                }
            }
        }
        #endregion

        #region Handlers
        #region Update Handler
        private static void Client_OnUpdate(object sender, Telegram.Bot.Args.UpdateEventArgs e)
        {
            try
            {
                Update u = e.Update;
                #region Message Updates
                if (u.Message != null)
                {
                    #region Text messages
                    if (u.Message.Text != null)
                    {
                        #region Messages containing entities
                        if (u.Message.Entities.Count != 0)
                        {
                            #region Commands
                            if (u.Message.Entities[0].Type == MessageEntityType.BotCommand
                                && u.Message.Entities[0].Offset == 0)
                            {
                                #region Commands only
                                if (u.Message.Entities[0].Length == u.Message.Text.Length)
                                {
                                    handleCommandOnly(msg: u.Message, cmd: u.Message.Text);
                                }
                                #endregion
                            }
                            #endregion
                        }
                        #endregion

                        #region Text messages handling
                        handleTextMessage(u.Message);
                        #endregion
                    }
                    #endregion

                    #region System messages
                    #region New member
                    if (u.Message.NewChatMember != null)
                    {
                        #region Bot added to group
                        if (u.Message.NewChatMember.Id == me.Id)
                        {
                            handleBotJoinedGroup(u.Message);
                        }
                        #endregion
                    }
                    #endregion
                    #endregion
                }
            }
            catch (Exception ex)
            {
                client.SendTextMessageAsync(flomsId, "An error has occurred: \n" + ex.ToString() + "\n"
                    + ex.Message + "\n" + ex.StackTrace);
            }
            #endregion
        }
        #endregion

        #region Commands
        #region Commands Only
        private static void handleCommandOnly(Message msg, string cmd)
        {
            switch (cmd)
            {
                case "/start":
                case "/start" + botUsername:
                    IReplyMarkup rm = StartKeyboard.Markup;
                    client.SendTextMessageAsync(msg.Chat.Id, startMessage, replyMarkup: rm);
                    break;
            }
        }
        #endregion
        #endregion

        #region Text messages
        private static void handleTextMessage(Message msg)
        {
            if (waitingFor.ContainsKey(msg.Chat.Id))
            {
                if (msg.Text == CancelKeyboard.CancelButtonString)
                {
                    #region Return to old keyboard
                    switch (waitingFor[msg.Chat.Id])
                    {
                        case ClosedlistKeyboard.ClosedlistAddButtonString:
                        case ClosedlistKeyboard.ClosedlistEditButtonString:
                        case ClosedlistKeyboard.ClosedlistRemoveButtonString:
                            client.SendTextMessageAsync(msg.Chat.Id, getCurrentClosedlist(),
                                replyMarkup: ClosedlistKeyboard.Markup);
                            break;
                        case UnderdevKeyboard.UnderdevAddButtonString:
                        case UnderdevKeyboard.UnderdevEditButtonString:
                        case UnderdevKeyboard.UnderdevRemoveButtonString:
                            client.SendTextMessageAsync(msg.Chat.Id, getCurrentUnderdev(),
                                replyMarkup: UnderdevKeyboard.Markup);
                            break;
                    }
                    #endregion
                    waitingFor.Remove(msg.Chat.Id);
                    return;
                }
                switch (waitingFor[msg.Chat.Id])
                {
                    #region Closedlist
                    case ClosedlistKeyboard.ClosedlistAddButtonString:
                        string error;
                        if (addToClosedlist(msg.Text, out error))
                        {
                            client.SendTextMessageAsync(msg.Chat.Id, "Language added.");
                            client.SendTextMessageAsync(msg.Chat.Id, getCurrentClosedlist(),
                                replyMarkup: ClosedlistKeyboard.Markup);
                            waitingFor.Remove(msg.Chat.Id);
                        }
                        else client.SendTextMessageAsync(msg.Chat.Id,
                            error);
                        break;
                    case ClosedlistKeyboard.ClosedlistEditButtonString:

                        break;
                    case ClosedlistKeyboard.ClosedlistRemoveButtonString:

                        break;
                    #endregion

                    #region Underdev
                    case UnderdevKeyboard.UnderdevAddButtonString:

                        break;
                    case UnderdevKeyboard.UnderdevEditButtonString:

                        break;
                    case UnderdevKeyboard.UnderdevRemoveButtonString:

                        break;
                    #endregion
                }
                return;
            }
            switch (msg.Text)
            {
                #region Start keyboard
                case StartKeyboard.ClosedlistButtonString:
                    client.SendTextMessageAsync(msg.Chat.Id, getCurrentClosedlist(),
                        replyMarkup: ClosedlistKeyboard.Markup);
                    break;
                case StartKeyboard.UnderdevButtonString:
                    client.SendTextMessageAsync(msg.Chat.Id, getCurrentUnderdev(),
                        replyMarkup: UnderdevKeyboard.Markup);
                    break;
                case StartKeyboard.RefreshChannelMessageButtonString:
                    client.EditMessageTextAsync(channelUsername, messageIdClosedlist, getCurrentClosedlist());
                    client.EditMessageTextAsync(channelUsername, messageIdUnderdev, getCurrentUnderdev());
                    client.SendTextMessageAsync(msg.Chat.Id, "Message refreshed");
                    break;
                case StartKeyboard.BackToStartKeyboardButtonString:
                    client.SendTextMessageAsync(msg.Chat.Id, "Main menu", replyMarkup: StartKeyboard.Markup);
                    break;
                #endregion

                #region Closedlist keyboard
                case ClosedlistKeyboard.ClosedlistAddButtonString:
                    client.SendTextMessageAsync(msg.Chat.Id, 
                        "Send me the language you want to add in the following format: \n" + 
                        "Language name - Information",
                        replyMarkup: CancelKeyboard.Markup);
                    waitingFor.Add(msg.Chat.Id, ClosedlistKeyboard.ClosedlistAddButtonString);
                    break;
                case ClosedlistKeyboard.ClosedlistEditButtonString:

                    break;
                case ClosedlistKeyboard.ClosedlistRemoveButtonString:

                    break;
                #endregion

                #region Underdev keyboard
                case UnderdevKeyboard.UnderdevAddButtonString:

                    break;
                case UnderdevKeyboard.UnderdevEditButtonString:

                    break;
                case UnderdevKeyboard.UnderdevRemoveButtonString:

                    break;
                #endregion
            }
        }
        #endregion

        #region System messages
        #region Bot joined Group
        private static void handleBotJoinedGroup(Message msg)
        {
            client.SendTextMessageAsync(msg.Chat.Id, "Please do not add me to any groups!");
            client.LeaveChatAsync(msg.Chat.Id);
        }
        #endregion
        #endregion
        #endregion

        #region SQL methods
        #region Getters
        private static string getCurrentClosedlist()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(closedlistPhpUrl);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream resStream = response.GetResponseStream();
            using (StreamReader sr = new StreamReader(resStream))
            {
                string result = sr.ReadToEnd().Replace("<br>", "\n");
                result = result.Replace(":", ": ");
                result = "#closedlist\n" + result;
                if (result != "#closedlist\n") return result;
                else return "No entries in #closedlist yet";
            }
        }

        private static string getCurrentUnderdev()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(underdevPhpUrl);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream resStream = response.GetResponseStream();
            using (StreamReader sr = new StreamReader(resStream))
            {
                string result = sr.ReadToEnd().Replace("<br>", "\n");
                result = result.Replace(":", ": ");
                result = "#underdev\n" + result;
                if (result != "#underdev\n") return result;
                else return "No entries in #underdev yet";
            }
        }
        #endregion

        #region Closedlist
        #region Add
        private static bool addToClosedlist(string process, out string error)
        {
            string[] proc = process.Split('-');
            if (proc.Length != 2)
            {
                error = "Failed to add string, check format";
                return false;
            }
            string lang = proc[0].Trim();
            string info = proc[1].Trim();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(addClosedlistPhpUrl + "?lang=" + lang
                + "&info=" + info);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream resStream = response.GetResponseStream();
            using (StreamReader sr = new StreamReader(resStream))
            {
                string res = sr.ReadToEnd();
                if (res == "true")
                {
                    error = null;
                    return true;
                }
                else
                {
                    string[] ret = res.Replace("<br>","\n").Split('\n');
                    if (ret[0] == "1062")
                    {
                        error = "Failed to add language entry, it is already present. Try again.";
                    }
                    else
                    {
                        error = ret[1];
                    }
                    return false;
                }
            }
        }
        #endregion

        #region Edit
        
        #endregion
        #endregion
        #endregion
    }
}