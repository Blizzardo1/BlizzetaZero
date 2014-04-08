﻿/*‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾*\
|*  Copyright (C) 2014  Blizzeta Software                                   *|
|*                                                                          *|
|*  This program is free software: you can redistribute it and/or modify    *|
|*  it under the terms of the GNU General Public License as published by    *|
|*  the Free Software Foundation, either version 3 of the License, or       *|
|*  (at your option) any later version.                                     *|
|*                                                                          *|
|*  This program is distributed in the hope that it will be useful,         *|
|*  but WITHOUT ANY WARRANTY; without even the implied warranty of          *|
|*  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the           *|
|*  GNU General Public License for more details.                            *|
|*                                                                          *|
|*  You should have received a copy of the GNU General Public License       *|
|*  along with this program.  If not, see <http://www.gnu.org/licenses/>.   *|
\*__________________________________________________________________________*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using dbg = System.Diagnostics.Debug;

namespace BlizzetaZero.Kernel
{
    using RFC1459;
    using Scripts;

    #region Delegates

    /// <summary>
    /// Actions for Channels
    /// </summary>
    /// <param name="from"></param>
    /// <param name="channel"></param>
    /// <param name="target"></param>
    /// <param name="message"></param>
    public delegate void ChannelActionHandler ( User from, Channel channel, string target, string message );

    /// <summary>
    /// Who changed the topic?
    /// </summary>
    /// <param name="from"></param>
    /// <param name="channel"></param>
    /// <param name="newtopic"></param>
    public delegate void ChannelTopicHandler ( User from, Channel channel, string newtopic );

    /// <summary>
    /// Retreive the list of users in a channel
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="list"></param>
    public delegate void ChannelUserListHandler ( Channel channel, string[] list );

    /// <summary>
    /// Server side Messages
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="message"></param>
    public delegate void MessageHandler ( User from, string to, string message );

    /// <summary>
    /// Who changed their nick?
    /// </summary>
    /// <param name="from"></param>
    /// <param name="newnick"></param>
    public delegate void NickChangeHandler ( User from, string newnick );

    /// <summary>
    /// Server Data
    /// </summary>
    /// <param name="data"></param>
    public delegate void ServerMessageHandler ( string data );

    /// <summary>
    /// Our messages to go out
    /// </summary>
    /// <param name="to"></param>
    /// <param name="message"></param>
    public delegate void ZeroHandler ( string to, string message );

    #endregion Delegates

    public class Irc
    {
        public static readonly string StartupPath = Path.GetDirectoryName ( System.Reflection.Assembly.GetExecutingAssembly ( ).Location );

        //public static readonly string DriversPath = string.Format ( "{0}\\Drivers", StartupPath );
        public static readonly string CmdPath = string.Format ( "{0}\\Scripts", StartupPath );

        public static readonly string LogsPath = string.Format ( "{0}\\Logs", StartupPath );

        public List<Channel> Channels = new List<Channel> ( );
        private TcpClient client;
        private ReplyCode code;
        private ConsoleColor colour;
        private bool hasMOTD = false;
        private string host, nick, username, realname, server, message, motd, _dbgchan = "#BlizzetaDebug", _owner = "Blizzardo1";
        private Ident iDent;

        // Oh do you? well Ident your car! .__.
        private Thread identWorker;

        private Thread killerQueen;
        private string[] messagearray;
        private int port;
        private StreamReader reader;
        private string startchan, startkey;
        private Stream stream;
        private Thread worker;
        private StreamWriter writer;

        /// <summary>
        /// Initializes a new instance of the <see cref="Irc"/> class.
        /// </summary>
        /// <param name="nick">The nick.</param>
        /// <param name="realname">The realname.</param>
        /// <param name="username">The username.</param>
        /// <param name="channel">The channel.</param>
        /// <param name="key">The key.</param>
        public Irc ( string nick, string realname, string username, string channel, string key = "" )
        {
            this.nick = nick;
            this.realname = realname;
            this.username = username;
            if ( !string.IsNullOrEmpty ( channel ) ) startchan = channel;
            if ( !string.IsNullOrEmpty ( key ) ) startkey = key;
        }

        public TcpClient Client { get { return client; } }

        public ReplyCode Code { get { return code; } }

        public ConsoleColor Colour
        {
            get { return colour; }
            set { colour = value; }
        }

        public string Host { get { return host; } }

        public string Message { get { return message; } }

        public string[] MessageArray { get { return messagearray; } }

        public string Nick { get { return nick; } }

        public string Owner { get { return _owner; } }

        public StreamReader Reader { get { return reader; } }

        public string Realname { get { return realname; } }

        /* public Thread Worker { get { return worker; } } <- This is not really needed as we can control the thread internally, but it's
                                                              good practice to include a readonly property */

        public string Server { get { return server; } }

        public Stream Stream { get { return stream; } }

        public string Username { get { return username; } }

        public StreamWriter Writer { get { return writer; } }

        #region Events

        public event ChannelActionHandler OnChannelJoin;

        public event ChannelActionHandler OnChannelKick;

        public event ChannelActionHandler OnChannelMode;

        public event ChannelActionHandler OnChannelPart;

        public event ChannelUserListHandler OnChannelUserList;

        public event ServerMessageHandler OnMotd;

        public event NickChangeHandler OnNickChange;

        public event MessageHandler OnPrivateAction;

        public event MessageHandler OnPrivateMessage;

        public event MessageHandler OnPrivateNotice;

        public event MessageHandler OnPublicAction;

        public event MessageHandler OnPublicMessage;

        public event MessageHandler OnPublicNotice;

        public event ZeroHandler OnSendToChannel;

        public event ZeroHandler OnSendToUser;

        public event ChannelActionHandler OnServerQuit;

        public event ZeroHandler OnSetMode;

        public event ChannelTopicHandler OnTopicChange;

        #endregion Events

        public void ChangeNick ( string newnick )
        {
            Raw ( IrcCommands.Nick ( newnick ) );
        }

        /// <summary>
        /// Connects the specified host.
        /// </summary>
        /// <param name="host">The host.</param>
        /// <param name="port">The port.</param>
        public void Connect ( string host, int port = 6667 )
        {
            IrcReply.FormatMessage ( string.Format ( "FIRE UP {0}", host ), ConsoleColor.Yellow, true );
            try
            {
                this.host = host;
                this.port = port;

                this.client = new TcpClient ( );

                iDent = new Ident ( "Zero" );
                identWorker = new Thread ( iDent.InitServer ) { Name = "DaQueen", IsBackground = false };
                identWorker.Start ( );

                killerQueen = new Thread ( KillItWithFire ) { Name = "KillerQueen", IsBackground = false };
                killerQueen.Start ( );

                this.client.SendBufferSize = 4096;
                this.client.Connect ( host, port );
                this.stream = this.client.GetStream ( );
                this.reader = new StreamReader ( this.stream );
                this.writer = new StreamWriter ( this.stream );
                Listen ( );
            }
            catch ( Exception ex )
            {
                Format ( ex.ToString ( ), ConsoleColor.Red );
            }
        }

        /// <summary>
        /// Disconnects with specified reason.
        /// </summary>
        /// <param name="reason">The reason.</param>
        public void Disconnect ( string reason = "" )
        {
            if ( !string.IsNullOrEmpty ( reason ) )
                Raw ( IrcCommands.Quit ( reason ) );
            else
                Raw ( IrcCommands.Quit ( Global.DefaultQuit ) );
        }

        /// <summary>
        /// Formats the specified text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="colour">The colour.</param>
        /// <param name="args">The arguments.</param>
        public void Format ( string text, ConsoleColor colour, params object[] args )
        {
            IrcReply.FormatMessage ( string.Format ( text, args ), colour );
        }

        /// <summary>
        /// Gets the channel.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public Channel GetChannel ( string name )
        {
            return Channels.Find ( new Predicate<Channel> ( ( c ) =>
            {
                return c.Name == name;
            } ) );
        }

        /// <summary>
        /// Sends a command to the ScriptEngine
        /// </summary>
        /// <param name="command">The command</param>
        /// <param name="args">Additional arguments</param>
        public void IssueCommand ( Channel caller, User responsible, string command, params object[] args )
        {
            run ( command, args, caller.Name, responsible.Nick, false, false );
        }

        /// <summary>
        /// Issues a piped command
        /// </summary>
        /// <param name="caller">The channel it was sent from</param>
        /// <param name="responsible"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void IssuePipeCommand ( Channel caller, User responsible, string command, params object[] args )
        {
            run ( command, args, caller.Name, responsible.Nick, true, false );
        }

        /// <summary>
        /// Issues a private command
        /// </summary>
        /// <param name="responsible"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void IssuePrivateCommand ( User responsible, string command, params object[] args )
        {
            run ( command, args, string.Empty, responsible.Nick, false, true );
        }

        /// <summary>
        /// Join a Channel
        /// </summary>
        /// <param name="channel">Channel to join</param>
        /// <param name="key">Key if the channel has +k</param>
        public void Join ( string channel, string key )
        {
            Channel c = new Channel ( this, channel, key );
            c.Join ( );
            Channels.Add ( c );
        }

        /// <summary>
        /// Kick a user from a channel
        /// </summary>
        /// <param name="channel">Channel</param>
        /// <param name="nick">Person being kicked</param>
        /// <param name="reason">A reason why they should behave</param>
        public void Kick ( string channel, string nick, string reason = "Your attitude is not conductive to the desired environment" )
        {
            GetChannel ( channel ).Kick ( nick, reason );
        }

        /// <summary>
        /// Listen on an infinite loop
        /// </summary>
        public void Listen ( )
        {
            this.worker = new Thread ( new ThreadStart ( delegate
            {
                this.writer.SendMessage ( IrcCommands.Nick ( nick ) );
                this.writer.SendMessage ( IrcCommands.User ( username, 8, realname ) );
                OnPrivateMessage += Irc_OnPrivateMessage;
                OnPrivateNotice += Irc_OnPrivateNotice;
                OnPrivateAction += Irc_OnPrivateAction;

                while ( this.client.Connected )
                {
                    try
                    {
                        string s = string.Empty;
                        while ( ( s = this.reader.ReadLine ( ) ) != null )
                        {
                            this.message = s;

                            string prefix, command;
                            string[] parameters;
                            this.messagearray = this.message.Split ( ' ' );

                            if ( this.messagearray[ 0 ].StartsWith ( ":" ) )
                                this.messagearray[ 0 ] = this.messagearray[ 0 ].Remove ( 0, 1 );

                            IrcCommands.ParseReply ( this.message, out prefix, out command, out parameters );

                            IrcMessage message = new IrcMessage ( prefix, command, parameters );

                            if ( string.IsNullOrEmpty ( this.server ) )
                                this.server = this.messagearray[ 0 ];
                            dbg.WriteLine ( string.Format ( "Server Reply: {0}", s ) );
                            /*
                                at System.String.Join(String separator, String[] value, Int32 startIndex, Int32 count)
                                at BlizzetaZero.Kernel.Extensions.NonZero(String[] arr) in g:\BreakerDev Suite 2012\Blizzeta Zero rev2\Blizzeta Zero rev2\Kernel\Extensions.cs:line 34
                                at BlizzetaZero.Kernel.Irc.<Listen>b__3() in g:\BreakerDev Suite 2012\Blizzeta Zero rev2\Blizzeta Zero rev2\Kernel\Irc.cs:line 277
                             */
                            string paramsmsg = string.Join ( " ", parameters.NonZero ( ) );
                            string[] parr = paramsmsg.Split ( ' ' );
                            User u = null;
                            Channel c = null;
                            try
                            {
                                u = ( ( !string.IsNullOrEmpty ( prefix ) ) ? new User ( prefix ) : null );
                                c = ( ( parameters[ 0 ].StartsWith ( "#" ) ) ? GetChannel ( parameters[ 0 ] ) : null );
                            }
                            catch { }

                            if ( prefix == this.server )
                            {
                                if ( Enum.TryParse<ReplyCode> ( command, out code ) )
                                    switch ( code )
                                    {
                                        case ReplyCode.RPL_TOPIC: Console.WriteLine (
                                            "Topic for {0}: {1}",
                                            parr[ 0 ],
                                            paramsmsg.Substring ( parr[ 0 ].Length + 2 ) );
                                            break;

                                        case ( ReplyCode ) 333: IrcReply.FormatMessage (
                                            string.Format ( "Topic in {0} set by {1} on {2:ddd MMM dd, yyyy HH:mm:ss}",
                                            parr[ 0 ],
                                            parr[ 1 ],
                                            Global.UnixTimeStampToDateTime ( double.Parse ( parr[ 2 ] ) ) ),
                                            ConsoleColor.Gray ); break; // Topic Who time?
                                        case ReplyCode.RPL_NAMESREPLY: IrcReply.FormatMessage ( message, ConsoleColor.DarkMagenta ); break;
                                        case ReplyCode.RPL_ENDOFNAMES: IrcReply.FormatMessage ( message, ConsoleColor.DarkCyan ); break;

                                        case ReplyCode.RPL_MOTD:
                                            if ( OnMotd != null )
                                            {
                                                string ms = string.Join ( " ", messagearray, 4, messagearray.Length - 4 );
                                                if ( ms.Length > 1 ) motd += string.Format ( "{0}\r\n", ms );
                                                OnMotd ( ms );
                                            }
                                            break;

                                        case ReplyCode.RPL_MOTDSTART:
                                            motd = string.Empty;
                                            break;

                                        case ReplyCode.RPL_ENDOFMOTD:
                                            hasMOTD = true;
                                            Raw ( IrcCommands.Mode ( nick, "+B" ) );
                                            Raw ( IrcCommands.Umode ( "+B" ) );
                                            Raw ( IrcCommands.Umode2 ( "+B" ) );
                                            if ( !string.IsNullOrEmpty ( startchan ) )
                                            {
                                                c = new Channel ( this, startchan, startkey );
                                                c.Join ( );
                                                Channels.Add ( c );
                                            }
                                            break;

                                        case ReplyCode.RPL_WHOISUSER:
                                            Format ( this.message, ConsoleColor.Magenta );
                                            break;

                                        case ReplyCode.RPL_ENDOFWHOIS:
                                            Format ( this.message, ConsoleColor.DarkMagenta );
                                            break;
                                    }
                                else
                                {
                                    Console.WriteLine ( this.message );
                                }
                            }
                            else if ( command == "PING" )
                            {
                                dbg.WriteLine ( message.ToString ( ) );
                                string send = ( IrcCommands.Pong ( message.Parameters[ 0 ] ) );
                                dbg.WriteLine ( send );
                                writer.SendMessage ( send );
                            }
                            else
                            {
                                switch ( command )
                                {
                                    // :Prefix Command :Params
                                    // :Derp!~JoeDerp@DirtHost.net JOIN :#Channel
                                    case "JOIN":
                                        if ( OnChannelJoin != null )
                                            OnChannelJoin ( u, c, "", paramsmsg );
                                        break;

                                    case "PART":
                                        if ( OnChannelPart != null )
                                            OnChannelPart ( u, c, "", paramsmsg );
                                        break;

                                    case "MODE":
                                        if ( OnChannelMode != null )
                                            OnChannelMode ( u, c, "", paramsmsg );
                                        break;

                                    case "KICK":
                                        if ( OnChannelKick != null )
                                            OnChannelKick ( u, c, "", paramsmsg );
                                        break;

                                    case "NICK":
                                        if ( OnNickChange != null )
                                            OnNickChange ( u, paramsmsg );
                                        break;

                                    case "QUIT":
                                        if ( OnServerQuit != null )
                                            OnServerQuit ( u, c, "", paramsmsg );
                                        break;

                                    case "PRIVMSG":
                                        if ( parameters[ 0 ].StartsWith ( "#" ) )
                                        {
                                            //Lucifer.GeekShed.net PRIVMSG #chris :ACTION peeks in
                                            if ( parameters[ 1 ].StartsWith (
                                                string.Format ( "{0}ACTION", Constants.CtcpChar ) )
                                                && parameters.Last ( ).EndsWith ( Constants.CtcpChar.ToString ( ) ) )
                                            {
                                                if ( OnPublicAction != null )
                                                    OnPublicAction ( u, c.Name, paramsmsg );
                                            }
                                            else
                                                if ( OnPublicMessage != null )
                                                    OnPublicMessage ( u, c.Name, paramsmsg );
                                        }
                                        else
                                            if ( OnPrivateMessage != null )
                                                OnPrivateMessage ( u, parameters[ 0 ], paramsmsg );
                                        break;

                                    case "NOTICE":
                                        if ( parameters[ 0 ].StartsWith ( "#" ) )
                                        {
                                            if ( OnPublicNotice != null )
                                                OnPublicNotice ( u, c.Name, paramsmsg );
                                        }
                                        else
                                            if ( OnPrivateNotice != null )
                                            {
                                                if ( parameters.Length > 0 )
                                                    OnPrivateNotice ( u != null ? u : new User ( "null!null@null" ),
                                                        parameters[ 0 ],
                                                        paramsmsg );
                                                else
                                                    OnPrivateNotice ( u != null ? u : new User ( "null!null@null" ),
                                                        this.nick,
                                                        "Notice returned 0" );
                                            }
                                        break;
                                }
                            }
                        }
                    }
                    catch ( Exception ex )
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine ( "{0}", ex );
                        Console.ResetColor ( );
                    }
                }
            } ) ) { Name = string.Format ( "{0}_thread", this.server ), IsBackground = false };
            this.worker.Start ( );
        }

        public void Mode ( string channel, string Modes )
        {
            GetChannel ( channel ).Mode ( Modes );
        }

        public void Part ( string channel, string reason = "Parted the channel" )
        {
            GetChannel ( channel ).Part ( reason );
        }

        public void Raw ( string message, params object[] args )
        {
            this.writer.SendMessage ( message, args );
        }

        public void SendAction ( string channel, string message )
        {
            Raw ( IrcCommands.Action ( channel, message ) );
        }

        public void SendErrorToFile ( Exception ex, string Nick )
        {
            if ( !Directory.Exists ( LogsPath ) )
                Directory.CreateDirectory ( LogsPath );
            var d = DateTime.Now;
            var r = d.ToString ( "yyyyMMddHHmmssfft" );

            var clientmsg = string.Format (
                "Something has terribly gone wrong. Please report this to {0}! Your new Fingerprint is {1}",
                _owner,
                r );

            var servermsg = string.Format (
                "Something has terribly gone wrong. Gathering UserInfo:\nNick: {0}\nFingerPrint: {1}\nTime Of incident: {2}\nException: {3}",
                Nick,
                r,
                d.ToString ( "dddd MMMM dd, yyyy :: HH:mm:ss" ),
                ex );

            SendMessage ( Nick, clientmsg );
            OpenWriteLog ( string.Format ( LogsPath + "\\{0}.blog", r ), servermsg );
        }

        public void SendMessage ( string channel, string message )
        {
            Raw ( IrcCommands.Privmsg ( channel, message ) );
        }

        public void SendNotice ( string target, string message )
        {
            Raw ( IrcCommands.Notice ( target, message ) );
        }

        private void Irc_OnPrivateAction ( User from, string to, string message )
        {
            message = message.Substring ( " ACTION ".Length );
            IrcReply.FormatMessage ( string.Format ( "* {0} {1}",
                from.Nick,
                message.Trim ( Constants.CtcpChar ),
                ConsoleColor.DarkCyan ) );
        }

        private void Irc_OnPrivateMessage ( User from, string to, string message )
        {
            string[] msg = message.Split ( ' ' );
            if ( msg[ 0 ].StartsWith ( "." ) )
            {
                // This is a private command, We won't display messages here
                string command = msg[ 0 ].Substring ( 1 );
                object[] arguments = msg.Length > 1 ? string.Join ( " ", msg, 1, msg.Length - 1 ).Split ( ' ' ) : new object[] { };
                CoreCommands.Execute ( command, this, null, from.Nick, arguments );
            }
            else
                Format ( "<{0}> {1}", ConsoleColor.DarkRed, from.Nick, message );
        }

        private void Irc_OnPrivateNotice ( User from, string to, string message )
        {
            Format ( "<{0}> => {1} ", ConsoleColor.DarkCyan, from.Nick, message );
        }

        private void KillItWithFire ( )
        {
            Thread.Sleep ( 120000 );
            if ( identWorker.IsAlive ) // WHY IS IT ALIVE FOR MORE THAN THAT MANY MILLISECONDS... WE MUST KILL IT NOW! >,>
            {
                IrcReply.FormatMessage ( "Sending Queen to kill the Ident, Cause Freddie Mercury!", ConsoleColor.Magenta, true );
                identWorker.Abort ( );
            }
        }

        private void OpenWriteLog ( string FileName, string Message, params object[] Params )
        {
            var w = new StreamWriter ( FileName );
            w.WriteLine ( Message, Params );
            w.Flush ( );
            w.Close ( );
        }

        /// <summary>
        /// Runs a command from an external script file
        /// </summary>
        /// <param name="command">The command to search for</param>
        /// <param name="commandSet">The arguments to pass to the script</param>
        /// <param name="destination">The destination, by default should be a channel, unless pm is true</param>
        /// <param name="target">the target of the nick that has called the command</param>
        /// <param name="pipe">Are we setting up a secondary command? This allows us to execute private commands in a public room</param>
        /// <param name="pm">Are we issuing a private message command?</param>
        private void run ( string command, object[] commandSet, string destination, string target, bool pipe = false, bool pm = false )
        {
            string args = string.Empty;
            Guid g = Guid.NewGuid ( );
            var sc = new ScriptContext ( );
            var se = new ScriptEngine ( );

            try
            {
                sc.server = this;
                var noMod = new List<object> ( );

                // TODO: Shorten this statement
                if ( commandSet.Length > 0 )
                    for ( int i = 0; i < commandSet.Length; i++ )
                    {
                        args += string.Format ( "[{0}]; ", commandSet[ i ] );
                        noMod.Add ( commandSet[ i ] );
                    }

                if ( destination.StartsWith ( "#" ) && pm == false )
                    sc.channel = GetChannel ( destination );
                else
                    destination = target;

                sc.nick = target;

                sc.Arguments = noMod;
                args = args.TrimEnd ( ' ' );

                string cmdlet = string.Format ( "{0}\\{1}.cs", CmdPath, command );
                var cmdi = se.GetInterface ( cmdlet );

                if ( cmdi == null )
                {
                    SendMessage ( destination, "Unknown Command" );
                    return;
                }
                else
                {
                    //pc ( "[{0:HH:mm:ss} {1}] {2}", ConsoleColor.Green, DateTime.Now, destination, target );
                    Format ( "Name: {0} - Help: {1} - Usage: {2} - More: {3} - Version: {4} - Permission: {5}",
                        ConsoleColor.DarkGreen,
                        cmdi.Name,
                        cmdi.Help,
                        cmdi.Usage,
                        cmdi.More,
                        cmdi.Version,
                        cmdi.Permission );

                    if ( cmdi.IsPublic )
                    {
                        if ( uac.GetPermission ( target, this ) <= cmdi.Permission )
                            se.Run ( cmdlet, ref sc );
                        else
                            SendMessage ( target,
                                       string.Format ( "You do not have permission to execute the command {0}", command ) );
                    }
                    else if ( pipe )
                    {
                        if ( uac.GetPermission ( target, this ) <= cmdi.Permission )
                            se.Run ( cmdlet, ref sc );
                    }
                    else if ( pm )
                    {
                        if ( uac.GetPermission ( target, this ) <= cmdi.Permission )
                            se.Run ( cmdlet, ref sc );
                        else
                            SendMessage ( target,
                                       string.Format ( "You do not have permission to execute the command {0}", command ) );
                    }
                    else if ( pipe && pm )
                        SendMessage ( target, "Pipe command cannot be used privately" );
                    else
                    {
                        SendMessage ( destination,
                                          string.Format ( "The command \"{0}\" can only be used privately!", command ) );
                    }
                }
            }
            catch ( Exception ex )
            {
                IrcReply.FormatMessage ( string.Format ( "{0}", ex.Message ), ConsoleColor.Red );
                SendMessage ( destination, string.Format ( "{0} -> Command \"{1}\" not found!", target, command ) );
                return;
            }
            SendMessage ( _dbgchan,
                string.Format ( "Command: {0}; Arguments: [{1}]; Callee: [{2}]; Destination: \"{3}\"; Fingerprint: [{4}]",
                    command,
                    ( args.Length > 0 ? args : "{{ null }}" ),
                    target,
                    destination,
                    g ) );
        }
    }

    public class IrcMessage
    {
        private string command;
        private string[] parameters;
        private string prefix;

        public IrcMessage ( string prefix, string command, string[] args )
        {
            this.prefix = prefix;
            this.command = command;
            this.parameters = args;
        }

        public string Command { get { return command; } }

        public string[] Parameters { get { return parameters; } }

        public string Prefix { get { return prefix; } }

        public override string ToString ( )
        {
            return string.Format ( "{0} {1} {2}", prefix, command, string.Join ( " ", parameters ) );
        }
    }
}