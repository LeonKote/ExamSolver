using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamSolver
{
	class Course
	{
		public string Name;
		public Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>> Sections;

		public Course()
		{
			Sections = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>>();
		}

		public void Put(string sectionName, string topicName, string taskId, Dictionary<string, string> answers)
		{
			if (Sections.ContainsKey(sectionName))
			{
				if (Sections[sectionName].ContainsKey(topicName))
				{
					if (Sections[sectionName][topicName].ContainsKey(taskId))
						Sections[sectionName][topicName][taskId] = answers;
					else Sections[sectionName][topicName].Add(taskId, answers);
				}
				else
				{
					Sections[sectionName].Add(topicName, new Dictionary<string, Dictionary<string, string>>());
					Sections[sectionName][topicName].Add(taskId, answers);
				}
			}
			else
			{
				Sections.Add(sectionName, new Dictionary<string, Dictionary<string, Dictionary<string, string>>>());
				Sections[sectionName].Add(topicName, new Dictionary<string, Dictionary<string, string>>());
				Sections[sectionName][topicName].Add(taskId, answers);
			}
		}

		public bool Contains(string sectionName)
		{
			return Sections.ContainsKey(sectionName);
		}

		public bool Contains(string sectionName, string topicName)
		{
			return Sections.ContainsKey(sectionName) && Sections[sectionName].ContainsKey(topicName);
		}
	}
}
