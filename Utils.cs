using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ExamSolver
{
	class Utils
	{
		public static bool Match(string str1, string str2)
		{
			int len = Math.Min(str1.Length, str2.Length);
			if (len == 0) return false;
			return str1.Substring(0, len) == str2.Substring(0, len);
		}

		public static bool MatchAnswer(string str1, string str2)
		{
			string tmp;

			if (str2.Length > str1.Length)
			{
				tmp = str2;
				str2 = str1;
				str1 = tmp;
			}

			if (str2.Length == 0 || str1.Substring(0, str2.Length) != str2.Substring(0, str2.Length)) return false;
			if (str1.Length > str2.Length) return Regex.IsMatch(str1[str2.Length].ToString(), "[^A-Za-z0-9]");
			else return true;
		}

		public static string GetId(string str)
		{
			return str.Substring(str.LastIndexOf('-') + 1);
		}
	}
}
