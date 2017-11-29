//
//  SKUA2OBJ.cs
//
//  Author:
//       Stephan Donndorf <stephan.donndorf@googlemail.com>
//
//  Copyright (c) 2017 Stephan Donndorf
//

using System;
using System.Globalization;
using System.Threading;

namespace SKUA2OBJ
{
    class SKUA2OBJ
    {
		static void Main(string[] args)
        {
			if (args.Length < 2)
			{
				Console.WriteLine("Wrong number of arguments! Canceling...");
				Console.WriteLine("Usage: SKUA2OBJ <SKUA-GOCAD surface file> <output OBJ file> <shift (optional format: X/Y/Z)>...");
				Environment.Exit(-1);
			}

			// change globalization information of this porgram (especially the file formats!)
			Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

			// no shift if there is no argument
			string shift = "0/0/0";
			if (args.Length > 2)
				shift = args[2];

			try
			{
				new SKUA2OBJConversion().convertSKUAFile(args[0], args[1], shift);
			}
			catch (Exception ex)
			{
				Console.WriteLine("An error occurs during conversation!");
				Console.WriteLine(ex.Message);
				foreach (string key in ex.Data.Keys)
					Console.WriteLine(key + " - " + ex.Data[key]);
			}
		}
    }
}
