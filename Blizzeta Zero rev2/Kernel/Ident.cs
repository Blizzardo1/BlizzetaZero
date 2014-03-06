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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BlizzetaZero.Kernel
{
    public class Ident
    {
        private readonly TcpListener listener;
        private readonly string userID;

        public Ident(string userID)
        {
            this.userID = userID;
            listener = new TcpListener(IPAddress.Any, 113);
        }

        public void InitServer()
        {
            try
            {
                listener.Start();
                TcpClient client = listener.AcceptTcpClient();
                listener.Stop();
                StreamReader reader = new StreamReader(client.GetStream());
                StreamWriter writer = new StreamWriter(client.GetStream());
                string s = reader.ReadLine();

                IrcReply.FormatMessage(string.Format("Fetched Ident! Ident is {0}. Sending a reply now...", s), ConsoleColor.Yellow);
                writer.SendMessage("{0} : USERID: UNIX : {1}", s, userID);
                IrcReply.FormatMessage("Sent!", ConsoleColor.Green);
                IrcReply.FormatMessage("Disconnecting from {{ Ident }}", ConsoleColor.Yellow);
            }
            catch (SocketException se)
            {
                IrcReply.FormatMessage(se.Message, ConsoleColor.Red, true);
            }
            catch (Exception ex)
            {
                IrcReply.FormatMessage(ex.Message, ConsoleColor.Red, true);
            }
        }
    }
}
