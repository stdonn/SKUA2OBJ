//
//  SKUA2OBJConversion.cs
//
//  Author:
//       Stephan Donndorf <stephan.donndorf@googlemail.com>
//
//  Copyright (c) 2017 Stephan Donndorf
//

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SKUA2OBJ
{
	public class SKUA2OBJConversion
	{
		protected void onFileNotFound(string path)
		{
			FileNotFoundException ex = new FileNotFoundException("Specified file doesn't exist!");
			ex.Data.Add("Time", DateTime.Now);
			ex.Data.Add("File", path);

			throw ex;
		}

		protected void onWrongShiftFormat(string shift, string text)
		{
			Exception ex = new Exception(text);
			ex.Data.Add("Time", DateTime.Now);
			ex.Data.Add("shift", shift);

			throw ex;
		}

		public bool convertSKUAFile(string inFile, string outFile, string shift)
		{
			if (!File.Exists(inFile))
			{
				onFileNotFound(inFile);
				return false;
			}

			Dictionary<string, int> association = new Dictionary<string, int>();
			double sx, sy, sz;
			string[] split = shift.Split('/');

			if (split.Length < 3)
			{
				onWrongShiftFormat(shift, "Wrong shift format! Use X/Y/Z instead!");
				return false;
			}

			try
			{
				sx = Convert.ToDouble(split[0]);
				sy = Convert.ToDouble(split[1]);
				sz = Convert.ToDouble(split[2]);
			}
			catch (FormatException)
			{
				onWrongShiftFormat(shift, "Shift numbers cannot be converted to double! Maybe us point instead of comma as decimal separator!");
				return false;
			}
			catch (OverflowException)
			{
				onWrongShiftFormat(shift, "One of the numbers exceeds the range of double format. Cannot continue.");
				return false;
			}

			Console.WriteLine("Shifting objects by: X = {0} ; Y = {1} ; Z = {2} ;", sx, sy, sz);

			Stopwatch watch = new Stopwatch();
			watch.Start();

			Console.WriteLine("Start reading SKUA-GOCAD surface file and convert it to OBJ format...");
			StringBuilder objData = new StringBuilder(4096);
			objData.Append("# These file was converted from a SKUA-GOCAD surface file!\n");
			objData.Append("# It was created by SKUA2OBJ from Stephan Donndorf\n");

			int vertices = 0;

			// read file line by line and convert it to obj...
			string[] lines = File.ReadAllLines(inFile);
			Console.WriteLine("Found {0} lines in SKUA-GOCAD file...", lines.Length);
			foreach (string line in lines)
			{
				/*string trimed = line.TrimEnd();

				// line == "TFACE" -> add new group as part number
				if (trimed == "TFACE")
				{
					objData.AppendLine("g Part_" + part.ToString());
					objData.AppendLine("s off");
					part++;
				}*/

				string[] parts = line.Split(' ');
				if (parts.Length < 2)
				{
					//Console.WriteLine( "< 2" );
					continue;
				}

				bool failure = false;
				try
				{
					switch (parts[0])
					{
						case ("VRTX"):
						case ("PVRTX"):
							if (parts.Length < 5)
							{
								failure = true;
								continue;
							}

							++vertices;
							association[parts[1]] = vertices;
							// objData.AppendLine("# Vertices-Nr: " + (++vertices).ToString());
							objData.Append("v " + (Convert.ToDouble(parts[2]) + sx).ToString());
							objData.Append(" " + (Convert.ToDouble(parts[4]) + sz).ToString());

							// ATTENTION position of y and z changed, additionally y is negative!
							objData.Append(" " + ((-1) * (Convert.ToDouble(parts[3]) + sy)).ToString());
							objData.Append("\n");

							break;

						case("ATOM"):
							if (parts.Length < 2)
							{
								failure = true;
								continue;
							}

							try
							{
								association[parts[1]] = Convert.ToInt32(association[parts[2]]);
							}
							catch (Exception)
							{
								Console.WriteLine("Cannot associate vertices! Skipping line {0}", line);
								break;
							}

							break;

						case ("TRGL"):
							if (parts.Length < 4)
							{
								failure = true;
								break;
							}

							// if ((Convert.ToInt32(parts[1]) > vertices) || (Convert.ToInt32(parts[2]) > vertices) || (Convert.ToInt32(parts[3]) > vertices)
							if (!((association.ContainsKey(parts[1])) && (association.ContainsKey(parts[2])) && (association.ContainsKey(parts[3]))))
							{
								Console.WriteLine("One TringleNR is not in the list of vertices! Ignoring line...");
								Console.WriteLine("Line: {0} -- Vertices: {1}", line, vertices);
								break;
							}

							if ((association[parts[1]] == association[parts[2]]) || (association[parts[1]] == association[parts[3]]) || (association[parts[2]] == association[parts[3]]))
							{
								Console.WriteLine("Triangles with 2 identical cornerpoints found. Ignoring...");
								Console.WriteLine("Line: {0}", line);
								break;
							}

							objData.AppendLine("f " + association[parts[1]] + " " + association[parts[2]] + " " + association[parts[3]]);
							break;

						case ("name:"):
							if (parts.Length < 2)
							{
								failure = true;
								continue;
							}

							objData.AppendLine("o " + parts[1]);
							break;

						default:
							/*Console.Write("Default");
							foreach (string p in parts)
							{
								Console.Write(" - {0}", p);
							}
							Console.WriteLine("");*/
							// Console.WriteLine( "default" );
							break;
					}

					/*Console.Write("association keys");
					foreach (string key in association.Keys)
						Console.Write(" - {0}", key);

					Console.WriteLine("");*/
				}

				catch (IndexOutOfRangeException ex)
				{
					Console.WriteLine("Index out of range Exception occured!");
					Console.WriteLine(ex.Message);
					foreach (string key in ex.Data.Keys)
						Console.WriteLine(key + " - " + ex.Data[key]);
					Console.WriteLine("Conversation line: \"" + line + "\"");
					return false;
				}
				catch (Exception ex)
				{
					Console.WriteLine("An exception occured!");
					Console.WriteLine(ex.Message);
					foreach (string key in ex.Data.Keys)
						Console.WriteLine(key + " - " + ex.Data[key]);
					Console.WriteLine("Conversation line: \"" + line + "\"");
					return false;
				}
				if (failure)
				{
					Console.WriteLine("WARNING: Cannot read line:");
					Console.WriteLine(line);
					return false;
				}
			}
			     
			Console.WriteLine("Start writing output OBJ file...");
			File.WriteAllText(outFile, objData.ToString());

			watch.Stop();
			Console.WriteLine("Finished conversion to obj file...");
			Console.WriteLine("Time: {0} ms", watch.ElapsedMilliseconds);

			return true;
		}
	}
}
