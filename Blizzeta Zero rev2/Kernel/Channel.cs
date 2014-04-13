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

namespace BlizzetaZero.Kernel
{
    using RFC1459;

    public struct Ban
    {
        public string Mask;
        public string SetBy;
        public string Timestamp;
    }

    public class Channel
    {
        private List<Ban> banlist = new List<Ban> ( );
        private string channel, key, modes, topic;
        private Irc parent;
        private List<User> userlist = new List<User> ( );

        public Channel ( Irc Parent, string Name, string Key )
        {
            this.parent = Parent;
            this.channel = Name;
            this.key = Key;
            this.modes = string.Empty;
        }

        public string Name { get { return channel; } }

        public string Topic { get { return topic; } }

        public List<User> Userlist { get { return userlist; } }

        public void Join ( )
        {
            parent.Raw ( IrcCommands.Join ( channel, key ) );
        }

        public void Kick ( string nick, string reason )
        {
            parent.Raw ( IrcCommands.Kick ( channel, nick, reason ) );
        }

        public void Mode ( string Modes )
        {
            parent.Raw ( IrcCommands.Mode ( channel, Modes ) );
        }

        public void Part ( string reason )
        {
            parent.Raw ( IrcCommands.Part ( channel, reason ) );
        }

        public void SendAction ( string message )
        {
            parent.Raw ( IrcCommands.Action ( channel, message ) );
        }

        public void SendMessage ( string message )
        {
            parent.Raw ( IrcCommands.Privmsg ( channel, message ) );
        }

        public void SendNotice ( string message )
        {
            parent.Raw ( IrcCommands.Notice ( channel, message ) );
        }

        public override string ToString ( )
        {
            return this.channel;
        }
        
        // Strip code from this file and move back to Irc.cs
        public void DisplayChannelJoin ( User from, string target, string message )
        {
            userlist.Add ( from );
            IrcReply.FormatMessage ( string.Format ( "{0} {1}@{2} has joined {3}", from.Nick, from.Username, from.Host, this.channel ), ConsoleColor.Cyan );
            parent.Raw ( IrcCommands.Notice ( from.Nick, string.Format ( "Welcome to {0}, {1}! Enjoy your stay! :3", this.channel, from.Nick ) ) );
        }

        public void DisplayChannelKick ( User from, string target, string message )
        {
            userlist.Remove ( new User ( target ) );
            IrcReply.FormatMessage ( string.Format ( "{0} has kicked {1} from {2} {{ {3} }}", from.Nick, target, this.channel, message ), ConsoleColor.Red );
        }

        public void DisplayChannelMode ( User from, string target, string message )
        {
            IrcReply.FormatMessage ( string.Format ( "{0} sets mode {1} in {2}", from.Nick, message, this.channel ), ConsoleColor.Gray );
        }

        public void DisplayChannelPart ( User from, string target, string message )
        {
            userlist.Remove ( from );
            IrcReply.FormatMessage ( string.Format ( "{0} has left {1} {{ {2} }}", from.Nick, this.channel, message ), ConsoleColor.DarkRed );
        }

        public void RecieveChannelUserList ( string[] list )
        {
            for ( int i = 0; i < list.Length; i++ )
                userlist.Add ( new User ( list[ i ] ) );
        }

        public void RecievePublicAction ( User from, string to, string message )
        {
            message = message.Substring ( " ACTION ".Length );
            IrcReply.FormatMessage ( string.Format ( "[{0}] * {1} {2}", this.channel, from.Nick, message.Trim ( Constants.CtcpChar ), ConsoleColor.DarkCyan ) );
        }

        public void RecievePublicMessage ( User from, string to, string message )
        {
            string[] msg = message.Split ( ' ' );

            // Parse as a command
            if ( msg[ 0 ].StartsWith ( "-" ) )
            {
                // -ExternalCommand Params
                IrcReply.FormatMessage ( string.Format ( "[{0}] <{1}> {2}", this.channel, from.Nick, message ), ConsoleColor.Magenta );

                string command = msg[ 0 ].Substring ( 1 );
                object[] arguments = msg.Length > 1 ? string.Join ( " ", msg, 1, msg.Length - 1 ).Split ( ' ' ) : new object[] { };

                parent.IssueCommand ( this, from, command, arguments );
            }
            else if ( msg[ 0 ].StartsWith ( "+" ) )
            {
                IrcReply.FormatMessage ( string.Format ( "[{0}] <{1}> {2}", this.channel, from.Nick, message ), ConsoleColor.Yellow );
                string command = msg[ 0 ].Substring ( 1 );
                object[] arguments = msg.Length > 0 ? string.Join ( " ", msg, 1, msg.Length - 1 ).Split ( ' ' ) : new object[] { };
                CoreCommands.Execute ( command, this.parent, this, from.Nick, arguments );
            }
            else // Just display the message in a specified colour
                IrcReply.FormatMessage ( string.Format ( "[{0}] <{1}> {2}", this.channel, from.Nick, message ), ConsoleColor.Green );
        }

        public void RecievePublicNotice ( User from, string to, string message )
        {
            IrcReply.FormatMessage ( string.Format ( "[{0}] :: [{1}] -> {2} ", this.channel, from.Nick, message ), ConsoleColor.DarkMagenta );
        }

        public void SendToChannel ( string to, string message )
        {
            this.parent.Writer.SendMessage ( IrcCommands.Privmsg ( to, message ) );
        }

        public void DisplayQuit ( User from, string target, string message )
        {
            userlist.Remove ( from );
            IrcReply.FormatMessage ( string.Format ( "{0} {1}@{2} has disconnected {{ {3} }}", from.Nick, from.Username, from.Host, message ), ConsoleColor.DarkRed );
        }

        public void ChangeTopic ( User from, string newtopic )
        {
            topic = newtopic;
            IrcReply.FormatMessage ( string.Format ( "{0} has changed topic in {1} :: {2}", from.Nick, this.channel, newtopic ), ConsoleColor.DarkCyan );
        }
        // End of Move
    }
}