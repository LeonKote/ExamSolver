using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ExamSolver
{
	enum MessageType
	{
		SaveSuccess,
		SaveNotCompleted,
		SolveSuccess,
		SolvePart,
		SolveNotInBase
	}

	class Logger
	{
		TextBox log;
		ComboBox combo;

		public Logger(TextBox log, ComboBox combo)
		{
			this.log = log;
			this.combo = combo;
		}

		public void Log(string logger, string msg)
		{
			log.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "][" + logger + "] " + msg);
			log.AppendText(Environment.NewLine);
		}

		public void Log(int topicIdx, string msg)
		{
			log.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "][" + combo.Items[topicIdx] + "] " + msg);
			log.AppendText(Environment.NewLine);
		}
	}
}
