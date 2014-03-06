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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace BlizzetaZero.Kernel
{
    using RFC1459;
    using Exceptions;

    /*
     * I will be the very best, Like no one ever was.
     * To catch them is my real test.
     * To train them is my cause.
     * I will travel across the land, searching far and wide.
     * Each pokémon, to understand the power that's inside.
     * ->   Pokémon! It's you and me, I know it's my destiny!
     * ->   Pokémon, Oh, you're my best friend, in a world we must defend.
     * ->   Pokémon, a heart so true, our courage will pull us through!
     * ->   You teach me and i'll teach you, Pokémon!
     * ->   Gotta catch 'em all!
     * ->   POKÉMON!
     */

    public class Program
    {
        private static void SetCharSet()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("Output encoding now changed to {0}", Console.OutputEncoding.EncodingName);
        }

        static void Main(string[] args)
        {
            Console.WindowWidth += 60;

            CoreCommands.IncludeBuiltInCommands ();
            SetCharSet();
            Irc irc = new Irc("BlizzetaZero", "Blizzeta Zero Bot 7.0", "Blizzeta", "#noname");
            
            try
            {
                irc.Colour = ConsoleColor.Green;
                irc.OnMotd += irc_OnMotd;
                irc.Connect("irc.ringoflightning.net", 6667);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static void irc_OnMotd(string data)
        {
            IrcReply.FormatMessage(data, ConsoleColor.Yellow);
        }
    }
}