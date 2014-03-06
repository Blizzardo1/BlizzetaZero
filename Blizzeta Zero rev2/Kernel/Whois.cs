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
using System.Linq;
using System.Text;

namespace BlizzetaZero.Kernel
{
    public class WhoInfo
    {
        private string channel, ident, host, server, nick, realname;
        private int hopcount;
        private bool isAway, isOwner, isSop, isOp, isHop, isVoice, isIrcOp, isRegistered;
        
        public string Channel { get { return channel; } }
        public string Ident { get { return ident; } }
        public string Host { get { return host; } }
        public string Server { get { return server; } }
        public string Nick { get { return nick; } }
        public string Realname { get { return realname; } }

        public int HopCount { get { return hopcount; } }

        public bool IsIrcOp { get { return isIrcOp; } }
        public bool IsAway { get { return isAway; } }
        public bool IsRegistered { get { return isRegistered; } }
        public bool IsOwner { get { return isOwner; } }
        public bool IsAdmin { get { return isSop; } }
        public bool IsOp { get { return isOp; } }
        public bool IsHalfOp { get { return isHop; } }
        public bool IsVoice { get { return isVoice; } }

        private WhoInfo()
        {
            
        }

        public static WhoInfo Parse( IrcMessage msg )
        {
            WhoInfo wi = new WhoInfo ();
            // Prefix               CMD  Parameters
            //                           0          1         2        3                          4                  5         6   7  8
            // 0                     1   2          3         4        5                          6                  7         8   9  10
            // :Lucifer.GeekShed.net 352 Blizzardo1 #Blizzeta ~DrAlanD *.dhcp.nwtn.ct.charter.com Komma.GeekShed.net user99672 Hr& :0 realname

            string prefix = msg.Prefix;
            string command = msg.Command;
            string[] parameters = msg.Parameters;

            wi.channel = parameters[1];
            wi.ident = parameters[2];
            wi.host = parameters[3];
            wi.server = parameters[4];
            wi.nick = parameters[5];
            string umode = parameters[6];
            string[] realdata = parameters[7].Split(' ');
            wi.hopcount = int.Parse ( realdata[0] );
            wi.realname = string.Join ( " ", realdata, 1, realdata.Length - 1 );

            for(int i = 0; i < umode.Length; i++)
                switch ( umode[i] )
                {
                    case 'H': wi.isAway = false; break;
                    case 'G': wi.isAway = true; break;
                    case '~': wi.isOwner = true; break;
                    case '&': wi.isSop = true; break;
                    case '@': wi.isOp = true; break;
                    case '%': wi.isHop = true; break;
                    case '+': wi.isVoice = true; break;
                    case 'r': wi.isRegistered = true; break;
                    case '*': wi.isIrcOp = true; break;
                }
            return wi;
        }

        public static WhoInfo GetUser(Irc server, string nick )
        {
            bool gotWho = false;
            WhoInfo wi = null;
            while ( !gotWho )
            {
                RFC1459.ReplyCode code;
                string message, prefix, command;
                string[] parameters;

                message = server.Reader.ReadLine ();
                RFC1459.IrcCommands.ParseReply ( message, out prefix, out command, out parameters );
                if ( Enum.TryParse<RFC1459.ReplyCode> ( command, out code ) )
                    switch ( code )
                    {
                        case RFC1459.ReplyCode.RPL_WHOREPLY:
                            wi = WhoInfo.Parse ( new IrcMessage ( prefix, command, parameters ) );
                            break;
                        case RFC1459.ReplyCode.RPL_ENDOFWHO:
                            gotWho = true;
                            break;
                    }
            } 
            return wi;
        }

        public override string ToString()
        {
            string hostmask = string.Format ( "{0}!{1}@{2}", ident, nick, host );
            return hostmask;
        }
    }
}
