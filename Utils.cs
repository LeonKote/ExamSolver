using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

		public static string GetId(string str)
		{
			return str.Substring(str.LastIndexOf('-') + 1);
		}
	}
}
