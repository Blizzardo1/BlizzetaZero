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

namespace BlizzetaZero.Kernel
{
    public class Global
    {
        public const string Appname = "Blizzeta Codenamed Zero";
        public const string Core = "3.0";
        public const string Scripts = "2.0";
        public const string UrlLoc = "https://github.com/Blizzardo1/BlizzetaZero";
        public const string Version = "7.0";
        public static string DefaultQuit = string.Format ( "{0} {1}; Source Code: {2}", Appname, Version, UrlLoc );
        public static string Title = string.Format ( "{0} Version {1}", Appname, Version );

        public static DateTime UnixTimeStampToDateTime ( double unixTimeStamp )
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime ( 1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc );
            dtDateTime = dtDateTime.AddSeconds ( unixTimeStamp ).ToLocalTime ( );
            return dtDateTime;
        }
    }
}