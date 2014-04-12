/*‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾*\
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
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace BlizzetaZero.Kernel
{
    public class CoreCommands
    {
        private static Dictionary<string, Func<Irc, string, string, object[], int>> commands = new Dictionary<string, Func<Irc, string, string, object[], int>> ( );

        private static bool ignoreBan = false, ignoreSay = false, ignoreKick = false;

        private static int overridekey = 0;

        private static string userdb = Irc.StartupPath + "\\User.db";

        public static Dictionary<string, Func<Irc, string, string, object[], int>> Commands { get { return commands; } }

        public static string UserDB { get { return userdb; } }

        public static void AddCommand ( string command, Func<Irc, string, string, object[], int> function )
        {
            IrcReply.FormatMessage ( string.Format ( "Including {0}", command ), ConsoleColor.Green );
            commands.Add ( command, function );
        }

        public static string CheckS ( string name )
        {
            return name.EndsWith ( "s" ) ? name + "'" : name + "'s";
        }

        public static void Execute ( string command, Irc irc, Channel chan, string callee, object[] args )
        {
            Func<Irc, string, string, object[], int> func = commands[ command ];
            if ( chan != null )
                func.Invoke ( irc, chan.Name, callee, args );
            else
                func.Invoke ( irc, string.Empty, callee, args );
        }

        public static void IncludeBuiltInCommands ( )
        {
            // act
            AddCommand ( "act", new Func<Irc, string, string, object[], int> ( ( i, c, n, o ) =>
            {
                string msg = string.Join ( " ", o as string[], 2, o.Length - 2 );

                i.SendAction ( c, msg );
                i.Format ( "{0} sent {1} this action: {2}", ConsoleColor.DarkMagenta, n, c, msg );
                return 0;
            } ) );

            // ajoin
            AddCommand ( "ajoin", new Func<Irc, string, string, object[], int> ( ( i, c, n, o ) =>
            {
                int len = o.Length;
                bool alert = false;
                string cmd = string.Empty;
                string Formatted = string.Empty;

                if ( len > 0 )
                    cmd = o[ 0 ] as string;
                else
                {
                    i.SendMessage ( n, "What do you want me to do again with Ajoin?" );
                    alert = true;
                }

                if ( o.Length > 1 )
                {
                    Formatted = string.Format ( "AJOIN {0} {1}", cmd, c );
                }
                else
                    Formatted = string.Format ( "AJOIN {0}", cmd );

                if ( cmd == "add" )
                    if ( len > 1 )
                        i.SendMessage ( n, string.Format ( "Added {0} to Ajoin", c ) );
                    else
                    {
                        i.SendMessage ( n, "Nothing to add!" );
                        alert = true;
                    }
                else if ( cmd == "addall" )
                    i.SendMessage ( n, "Adding all open channels to Ajoin" );
                else if ( cmd == "del" )
                    if ( len > 1 )
                        i.SendMessage ( n, string.Format ( "Deleting {0} from Ajoin" ) );
                    else
                    {
                        i.SendMessage ( n, "Noting to delete!" );
                        alert = true;
                    }
                else if ( cmd == "list" )
                    i.SendMessage ( n, "Listing all available channels" );
                else if ( cmd == "clear" )
                    i.SendMessage ( n, "Cleared Channels" );

                if ( !alert )
                    i.SendMessage ( "NickServ", Formatted );

                return 0;
            } ) );

            // away
            AddCommand ( "away", new Func<Irc, string, string, object[], int> ( ( i, c, n, o ) =>
            {
                string amsg = string.Join ( " ", o as string[] );
                i.Raw ( RFC1459.IrcCommands.Away ( amsg ) );
                return 0;
            } ) );

            //ban
            AddCommand ( "ban", new Func<Irc, string, string, object[], int> ( ( i, c, n, o ) =>
            {
                if ( !ignoreBan )
                {
                    i.Raw ( RFC1459.IrcCommands.Ban ( c, o[ 0 ] as string ) );
                    commands[ "kick" ].Invoke ( i, c, n, o );
                }
                else
                    i.Format ( "Ignoring ban called by {0}", ConsoleColor.Red, n );
                return 0;
            } ) );

            // check [permission]
            AddCommand ( "check", new Func<Irc, string, string, object[], int> ( ( i, c, n, o ) =>
            {
                Dictionary<string, Func<int>> subcmd = new Dictionary<string, Func<int>> ( );
                subcmd.Add ( "permission", new Func<int> ( ( ) =>
                {
                    // TODO: Add permissions check
                    return 0;
                } ) );

                subcmd.Add ( "time", new Func<int> ( ( ) =>
                {
                    // TODO: Add configuration for callee
                    DateTime dt = DateTime.Now;
                    i.Format ( "Current Time: {0:dddd MMMM dd, yyyy} at {0:HH:mm:ss}", ConsoleColor.Yellow, dt );
                    i.GetChannel ( c ).SendMessage ( string.Format ( "{0} {1:dddd MMMM dd, yyyy} at {1:HH:mm:ss}", CheckS ( i.Owner ), dt ) );
                    return 0;
                } ) );
                subcmd.Add ( "uptime", new Func<int> ( ( ) =>
                {
                    i.Format ( "Uptime: {0}", ConsoleColor.DarkGreen );
                    i.GetChannel ( c ).SendMessage ( string.Format ( "{0}, I've been up for {1} {2}, {3} {4}, {5} {6}, and {7} {8}" ) );
                    return 0;
                } ) );

                return 0;
            } ) );
            // clear
            AddCommand ( "clear", new Func<Irc, string, string, object[], int> ( ( i, c, n, o ) =>
            {
                Console.Clear ( );
                return 0;
            } ) );
            // createdb
            AddCommand ( "createdb", new Func<Irc, string, string, object[], int> ( ( i, c, n, o ) =>
            {
                Dictionary<string, Func<int>> subcmd = new Dictionary<string, Func<int>> ( );

                subcmd.Add ( "activate", new Func<int> ( ( ) =>
                {
                    string key = o[ 1 ] as string;
                    string[] decryptedKey;

                    if ( ProductKey.ActivateKey ( key, out decryptedKey ) )
                    {
                        if ( !File.Exists ( userdb ) )
                        {
                            XmlDocument xDoc = new XmlDocument ( );
                            XmlNode xDeclaration = xDoc.CreateXmlDeclaration ( "1.0", "UTF-8", "yes" );
                            XmlNode xRoot = xDoc.CreateElement ( "Users" );
                            xDoc.AppendChild ( xDeclaration );
                            xDoc.AppendChild ( xRoot );
                            xDoc.Save ( userdb );
                            i.GetChannel ( c ).SendMessage ( string.Format ( "A new Database has been created and activated, thanks to {0} in {1} on {2: dddd MMMM dd, yyyy} at {2:hh:mm:ss tt}!", decryptedKey[ 0 ], decryptedKey[ 1 ], DateTime.FromBinary ( long.Parse ( decryptedKey[ 2 ] ) ) ) );
                        }
                        else
                            i.GetChannel ( c ).SendMessage ( "A database already exists and has been authenticated by me. :( If my Userbase is corrupt or you would like to clear it, please do +createdb override" );
                    }

                    return 0;
                } ) );

                //          0        1
                // createdb override key
                subcmd.Add ( "override", new Func<int> ( ( ) =>
                {
                    int key = ( new Random ( ) ).Next ( 10000, int.MaxValue );
                    if ( o.Length < 2 )
                    {
                        overridekey = key;
                        i.GetChannel ( c ).SendMessage ( string.Format ( "Please tell {0} to come override.", i.Owner ) );
                        i.SendMessage ( i.Owner, string.Format ( "Override Activation Key is {0}. This is here to prevent misuse of the createdb command. Please ignore this if your Database is not corrupted or you don't want to start over...", overridekey ) );
                    }
                    else
                    {
                        if ( int.Parse ( o[ 1 ] as string ) == overridekey )
                        {
                            File.Delete ( userdb );
                            i.SendMessage ( i.Owner, "Database has been destroyed." );
                        }
                        else
                        {
                            i.SendMessage ( i.Owner, string.Format ( "{0} has attempted to override the database! Their code was {1}, real code is {2}", n, o[ 1 ] as string, overridekey ) );
                        }
                    }

                    return 0;
                } ) );

                if ( o.Length > 0 )
                    try
                    {
                        subcmd[ o[ 0 ] as string ].Invoke ( );
                    }
                    catch ( Exception )
                    {
                        i.GetChannel ( c ).SendMessage ( "Invalid command in creatdb" );
                    }
                else
                {
                    if ( !File.Exists ( userdb ) )
                    {
                        string Code = ProductKey.GenerateProductKey ( n, c ).ProductID;
                        i.Format ( "Access Code is {0}", ConsoleColor.DarkGreen, Code );

                        System.IO.File.WriteAllText ( Irc.StartupPath + "\\code.esd", Code.ToString ( ) );
                        i.GetChannel ( c ).SendMessage ( string.Format ( "Access Code sent to {0}", i.Owner ) );
                    }
                    else
                        i.GetChannel ( c ).SendMessage ( "User Database already exists!!!" );
                }
                return 0;
            } ) );

            // get
            AddCommand ( "get", new Func<Irc, string, string, object[], int> ( ( i, c, n, o ) =>
            {
                return 0;
            } ) );

            // set
            AddCommand ( "set", new Func<Irc, string, string, object[], int> ( ( i, c, n, o ) =>
            {
                Dictionary<string, Func<int>> subcmd = new Dictionary<string, Func<int>> ( );

                subcmd.Add ( "kick", new Func<int> ( ( ) =>
                {
                    string val = ( o[ 1 ] as string ).ToLower ( );
                    if ( val == "on" || val == "true" )
                    {
                        ignoreKick = false;
                    }
                    else if ( val == "off" || val == "false" )
                    {
                        ignoreKick = true;
                    }
                    return 0;
                } ) );

                subcmd.Add ( "say", new Func<int> ( ( ) =>
                {
                    string val = ( o[ 1 ] as string ).ToLower ( );
                    if ( val == "on" || val == "true" )
                    {
                        ignoreSay = false;
                    }
                    else if ( val == "off" || val == "false" )
                    {
                        ignoreSay = true;
                    }
                    return 0;
                } ) );

                subcmd.Add ( "ban", new Func<int> ( ( ) =>
                {
                    string val = ( o[ 1 ] as string ).ToLower ( );
                    if ( val == "on" || val == "true" )
                    {
                        ignoreBan = false;
                    }
                    else if ( val == "off" || val == "false" )
                    {
                        ignoreBan = true;
                    }
                    return 0;
                } ) );

                return 0;
            } ) );

            // help
            AddCommand ( "help", new Func<Irc, string, string, object[], int> ( ( i, c, n, o ) =>
            {
                // +help [-mpuv] [command] [subcommand]
                /*
                 *  Options:
                 *      -m | --more         : More Command
                 *      -p | --permissions  : Permissions Command
                 *      -u | --usage        : Usage Command
                 *      -v | --version      : Version Command
                 */

                return 0;
            } ) );

            // join
            AddCommand ( "join", new Func<Irc, string, string, object[], int> ( ( i, c, n, o ) =>
            {
                bool chanExists = i.Channels.Exists ( new Predicate<Channel> ( ( ch ) =>
                {
                    return ch.Name == o[ 0 ] as string;
                } ) );

                if ( !chanExists )
                {
                    if ( o.Length > 1 )
                    {
                        i.Join ( o[ 0 ] as string, o[ 1 ] as string );
                        return 0;
                    }
                    else if ( o.Length > 0 )
                    {
                        i.Join ( o[ 0 ] as string, string.Empty );
                        return 0;
                    }
                    else
                        i.SendMessage ( c, "I can't join without a channel name" );
                }
                else
                {
                    i.SendMessage ( c, string.Format ( "I'm already in {0}!", o[ 0 ] as string ) );
                }
                return -1;
            } ) );

            // kick
            AddCommand ( "kick", new Func<Irc, string, string, object[], int> ( ( i, c, n, o ) =>
            {
                string[] ss = o as string[];
                if ( !ignoreKick )
                    i.Raw ( RFC1459.IrcCommands.Kick ( c, o[ 0 ] as string, string.Join ( " ", ss, 1, ss.Length - 1 ) ) );
                return 0;
            } ) );

            // list
            AddCommand ( "list", new Func<Irc, string, string, object[], int> ( ( i, c, n, o ) =>
            {
                Channel[] chans = i.Channels.ToArray ( );
                i.SendMessage ( n, "I am in these channels:" );
                foreach ( Channel chan in chans )
                {
                    i.SendMessage ( n, string.Format ( chan.Name ) );
                }
                return 0;
            } ) );

            // me
            AddCommand ( "me", new Func<Irc, string, string, object[], int> ( ( i, c, n, o ) =>
            {
                Dictionary<string, Func<int>> subcmd = new Dictionary<string, Func<int>> ( );
                XmlDocument xDoc = new XmlDocument ( );

                subcmd.Add ( "add", new Func<int> ( ( ) =>
                {
                    foreach ( User u in ReadUsers ( ) )
                    {
                        if ( u.name == n )
                        {
                            i.GetChannel ( c ).SendMessage ( string.Format ( "The Account for {0} already exists!", n ) );
                            return 1;
                        }
                    }

                    xDoc.Load ( userdb );
                    XmlNode xRoot = xDoc.SelectSingleNode ( "/Users" );
                    XmlNode xUser = xDoc.CreateElement ( "User" );

                    XmlAttribute aUser = xDoc.CreateAttribute ( "name" );
                    XmlAttribute aHost = xDoc.CreateAttribute ( "host" );
                    XmlAttribute aPermission = xDoc.CreateAttribute ( "permission" );
                    XmlAttribute aBan = xDoc.CreateAttribute ( "banned" );

                    i.Raw ( RFC1459.IrcCommands.Who ( n, false ) );

                    WhoInfo wi = WhoInfo.GetUser ( i, o[ 0 ] as string );

                    aUser.Value = wi.Nick;
                    aHost.Value = wi.Host;
                    aPermission.Value = Enum.GetName ( typeof ( Scripts.Permissions ), Scripts.Permissions.User );
                    aBan.Value = false.ToString ( );

                    xUser.Attributes.Append ( aUser );
                    xUser.Attributes.Append ( aHost );
                    xUser.Attributes.Append ( aPermission );
                    xUser.Attributes.Append ( aBan );

                    xRoot.AppendChild ( xUser );
                    xDoc.AppendChild ( xRoot );
                    xDoc.Save ( userdb );
                    i.GetChannel ( c ).SendMessage ( string.Format ( "Welcome {0}!", n ) );
                    return 0;
                } ) );

                subcmd.Add ( "del", new Func<int> ( ( ) =>
                {
                    xDoc.Load ( userdb );
                    XmlNodeList list = xDoc.SelectNodes ( "/Users/User" );

                    foreach ( XmlNode no in list )
                    {
                        string name = no.Attributes[ "name" ].Value;
                        if ( name == n )
                        {
                            i.Format ( "Selected {0}!", ConsoleColor.DarkGreen, name );
                            xDoc.SelectSingleNode ( "/Users" ).RemoveChild ( no );
                            xDoc.Save ( userdb );
                            i.GetChannel ( c ).SendMessage ( string.Format ( "Sorry to see you go {0}! :(", name ) );
                            break;
                        }
                    }
                    return 0;
                } ) );

                try
                {
                    if ( System.IO.File.Exists ( userdb ) )
                    {
                        subcmd[ o[ 0 ] as string ].Invoke ( );
                    }
                    else
                        i.GetChannel ( c ).SendMessage ( "There is no Database. Use +createdb to send an Access code to Blizzardo1 for a new File." );
                }
                catch ( Exception ex )
                {
                    i.GetChannel ( c ).SendMessage ( ex.Message );
                    Console.WriteLine ( ex );
                }

                return 0;
            } ) );

            // mode
            // TODO: add mode for moderation

            // nick
            AddCommand ( "nick", new Func<Irc, string, string, object[], int> ( ( i, c, n, o ) =>
            {
                i.Raw ( RFC1459.IrcCommands.Nick ( o[ 0 ] as string ) );
                return 0;
            } ) );

            // part
            AddCommand ( "part", new Func<Irc, string, string, object[], int> ( ( i, c, n, o ) =>
            {
                string[] format = o as string[];
                string channel = format[ 0 ];
                i.Part ( channel, string.Join ( " ", format, 1, format.Length - 1 ) );
                return 0;
            } ) );

            // quit
            AddCommand ( "quit", new Func<Irc, string, string, object[], int> ( ( i, c, n, o ) =>
            {
                i.Raw ( RFC1459.IrcCommands.Quit ( string.Join ( " ", o as string[] ) ) );
                return 0;
            } ) );

            // raw
            AddCommand ( "raw", new Func<Irc, string, string, object[], int> ( ( i, c, n, o ) =>
            {
                i.Raw ( string.Join ( " ", o ) );
                return 0;
            } ) );

            // reboot
            AddCommand ( "reboot", new Func<Irc, string, string, object[], int> ( ( i, c, n, o ) =>
            {
                System.Diagnostics.Process p = new System.Diagnostics.Process ( )
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo ( )
                    {
                        FileName = System.Reflection.Assembly.GetExecutingAssembly ( ).GetName ( ).Name + ".exe"
                    }
                };
                i.Disconnect ( "Rebooting!" );
                p.Start ( );
                Environment.Exit ( 0 );

                return 0;
            } ) );

            // say
            AddCommand ( "say", new Func<Irc, string, string, object[], int> ( ( i, c, n, o ) =>
            {
                if ( !ignoreSay )
                    i.GetChannel ( c ).SendMessage ( string.Join ( " ", o as string[] ) );
                return 0;
            } ) );

            // version
            AddCommand ( "version", new Func<Irc, string, string, object[], int> ( ( i, c, n, o ) =>
            {
                Dictionary<string, Func<int>> arguments = new Dictionary<string, Func<int>> ( );
                arguments.Add ( "core", new Func<int> ( ( ) =>
                {
                    i.GetChannel ( c ).SendMessage ( string.Format ( "{0}; Core Version {1}", Global.Title, Global.Core ) );
                    return 0;
                } ) );

                arguments.Add ( "scripts", new Func<int> ( ( ) =>
                {
                    i.GetChannel ( c ).SendMessage ( string.Format ( "{0}; Scripts Version {1}", Global.Title, Global.Scripts ) );
                    return 0;
                } ) );

                try
                {
                    arguments[ o[ 0 ] as string ].Invoke ( );
                }
                catch
                {
                    i.GetChannel ( c ).SendMessage ( string.Format ( "{0}; For more information, see \"core\" and \"scripts\"", Global.Title ) );
                }
                return 0;
            } ) );

            // weather [No API]
            // whois
            AddCommand ( "whois", new Func<Irc, string, string, object[], int> ( ( i, c, n, o ) =>
            {
                i.Raw ( RFC1459.IrcCommands.Whois ( o[ 0 ] as string ) );
                return 0;
            } ) );
        }

        public static void ReleaseCommand ( string command )
        {
            IrcReply.FormatMessage ( string.Format ( "Excluding {0}", command ), ConsoleColor.DarkRed );
            commands.Remove ( command );
        }

        private static IEnumerable<User> ReadUsers ( )
        {
            XmlDocument doc = new XmlDocument ( );
            doc.Load ( userdb );

            XmlNodeList list = doc.SelectNodes ( "/Users/User" );
            List<User> users = new List<User> ( );

            foreach ( XmlNode n in list )
            {
                string name = n.Attributes[ "name" ].Value, host = n.Attributes[ "host" ].Value, banned = n.Attributes[ "banned" ].Value, permission = n.Attributes[ "permission" ].Value;
                //Irc.Format("Name: {0}, Host: {1}, Banned: {2}, Permission: {3}", ConsoleColor.Green, name, host, banned, permission);
                users.Add ( new User { name = name, host = host, banned = banned, permission = permission } );
            }

            return users.ToArray ( );
        }

        private struct User
        {
            public string banned;
            public string host;
            public string name;
            public string permission;
        }
    }

    public class ProductKey
    {
        private string productID;

        public string ProductID { get { return productID; } }

        public static bool ActivateKey ( string key, out string[] decrypted )
        {
            string[] data = KeyCipher.Decrypt ( key, "blizzetazero70iamopensourcemadebyblizzardo1dontabuse(c)2014blizzardo1" ).Split ( ':' );
            if ( data.Length == 3 )
            {
                IrcReply.FormatMessage ( string.Format ( "Nick: {0}\r\nChannel: {1}\r\nDate: {2:dddd MMMM dd, yyyy} at {2:HH:mm:ss}", data[ 0 ], data[ 1 ], DateTime.FromBinary ( long.Parse ( data[ 2 ] ) ) ), ConsoleColor.DarkGray, true );
                decrypted = data;
                return true;
            }

            decrypted = null;
            return false;
        }

        public static ProductKey GenerateProductKey ( string nick, string channel )
        {
            DateTime dt = DateTime.Now;
            string data = string.Format ( "{0}:{1}:{2}", nick, channel, dt.ToBinary ( ) );

            return new ProductKey ( ) { productID = KeyCipher.Encrypt ( data, "blizzetazero70iamopensourcemadebyblizzardo1dontabuse(c)2014blizzardo1" ) };
        }

        public static class KeyCipher
        {
            // This constant string is used as a "salt" value for the PasswordDeriveBytes function calls.
            // This size of the IV (in bytes) must = (keysize / 8).  Default keysize is 256, so the IV must be
            // 32 bytes long.  Using a 16 character string here gives us 32 bytes when converted to a byte array.
            private const string initVector = "tu89geji340t89u2";

            // This constant is used to determine the keysize of the encryption algorithm.
            private const int keysize = 256;

            public static string Decrypt ( string cipherText, string passPhrase )
            {
                byte[] initVectorBytes = Encoding.ASCII.GetBytes ( initVector );
                byte[] cipherTextBytes = Convert.FromBase64String ( cipherText );
                PasswordDeriveBytes password = new PasswordDeriveBytes ( passPhrase, null );
                byte[] keyBytes = password.GetBytes ( keysize / 8 );
                RijndaelManaged symmetricKey = new RijndaelManaged ( );
                symmetricKey.Mode = CipherMode.CBC;
                ICryptoTransform decryptor = symmetricKey.CreateDecryptor ( keyBytes, initVectorBytes );
                MemoryStream memoryStream = new MemoryStream ( cipherTextBytes );
                CryptoStream cryptoStream = new CryptoStream ( memoryStream, decryptor, CryptoStreamMode.Read );
                byte[] plainTextBytes = new byte[ cipherTextBytes.Length ];
                int decryptedByteCount = cryptoStream.Read ( plainTextBytes, 0, plainTextBytes.Length );
                memoryStream.Close ( );
                cryptoStream.Close ( );
                return Encoding.UTF8.GetString ( plainTextBytes, 0, decryptedByteCount );
            }

            public static string Encrypt ( string plainText, string passPhrase )
            {
                byte[] initVectorBytes = Encoding.UTF8.GetBytes ( initVector );
                byte[] plainTextBytes = Encoding.UTF8.GetBytes ( plainText );
                PasswordDeriveBytes password = new PasswordDeriveBytes ( passPhrase, null );
                byte[] keyBytes = password.GetBytes ( keysize / 8 );
                RijndaelManaged symmetricKey = new RijndaelManaged ( );
                symmetricKey.Mode = CipherMode.CBC;
                ICryptoTransform encryptor = symmetricKey.CreateEncryptor ( keyBytes, initVectorBytes );
                MemoryStream memoryStream = new MemoryStream ( );
                CryptoStream cryptoStream = new CryptoStream ( memoryStream, encryptor, CryptoStreamMode.Write );
                cryptoStream.Write ( plainTextBytes, 0, plainTextBytes.Length );
                cryptoStream.FlushFinalBlock ( );
                byte[] cipherTextBytes = memoryStream.ToArray ( );
                memoryStream.Close ( );
                cryptoStream.Close ( );
                return Convert.ToBase64String ( cipherTextBytes );
            }
        }
    }
}