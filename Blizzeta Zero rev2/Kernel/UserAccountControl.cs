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
using System.Xml;

namespace BlizzetaZero.Kernel
{
    using Scripts;
    public class uac
    {
        private static Irc i;
        public static Permissions GetPermission( string Nick, Irc s )
        {
            i = s;
            User[] users = ReadUsers ();
            var availablenicks = new List<string> ( users.Select ( ( U ) => U.Name ) );

            if ( availablenicks.Contains ( Nick ) )
                foreach ( User user in users.Where ( user => user.Name == Nick ) )
                {
                    try
                    {
                        if ( user.Host ==  getUser(Nick).Host)
                        {
                            if ( !bool.Parse ( user.Banned ) )
                            {
                                return ( Permissions )Enum.Parse ( typeof ( Permissions ), user.Permission );
                            }
                            else
                            {
                                s.SendMessage ( Nick, "You're not allowed to use my Private Messaging system!" );
                            }
                        }
                        else
                        {
                            s.SendMessage ( Nick,
                                          string.Format (
                                              "You are {0}, however the host I have on record does not match! If you find this in error, please contact {1} for further assistance.",
                                              Nick, s.Owner ) );
                        }
                    }
                    catch ( Exception ex )
                    {
                        s.SendErrorToFile ( ex, Nick );
                        break;
                    }
                }
            return Permissions.Guest;
        }

        private static User[] ReadUsers()
        {
            var doc = new XmlDocument ();
            doc.Load ( Irc.StartupPath + "\\User.db" );

            XmlNodeList list = doc.SelectNodes ( "/Users/User" );
            var users = new List<User> ();

            if ( list != null )
                users.AddRange ( from n in ( from XmlNode n in list where n.Attributes != null select n ) where n.Attributes != null let name = n.Attributes["name"].Value let host = n.Attributes["host"].Value let banned = n.Attributes["banned"].Value let permission = n.Attributes["permission"].Value select new User { Name = name, Host = host, Banned = banned, Permission = permission } );

            return users.ToArray ();
        }

        private static User getUser( string name )
        {
            Kernel.User u = Kernel.User.GetUser ( i, name );
            return new User () { Name = u.Nick, Host = u.Host };
        }

        private struct User
        {
            public string Banned;
            public string Host;
            public string Name;
            public string Permission;
        }
    }
}
