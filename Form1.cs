using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ExamSolver
{
	public partial class Form1 : Form
	{
		bool logged = false;
		bool solve = false;
		bool wholeSection = false;

		Course course = new Course();
		Logger logger;

		List<HtmlElement> courses = new List<HtmlElement>();
		List<HtmlElement> sections = new List<HtmlElement>();
		List<HtmlElement> topics = new List<HtmlElement>();
		
		List<string> sectionLinks = new List<string>();

		public Form1()
		{
			InitializeComponent();
			comboBox1.SelectedIndex = 0;
			comboBox2.SelectedIndex = 0;
			comboBox3.SelectedIndex = 0;

			logger = new Logger(textBox1, comboBox3);
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			logger.Log("Program", "Starting shell...");
			webBrowser1.Navigate("https://exam1.urfu.ru/auth/saml/index.php");
		}

		private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
		{
			string url = e.Url.ToString();

			if (url == "https://exam1.urfu.ru/")
			{
				if (logged)
				{
					solve = false;
					wholeSection = false;

					comboBox1.Items.Clear();
					comboBox1.Items.Add("Выберите курс");

					comboBox1.SelectedIndex = 0;
					comboBox2.SelectedIndex = 0;
					comboBox3.SelectedIndex = 0;
					comboBox1.Enabled = true;
					button1.Enabled = true;
					button2.Enabled = true;
					button3.Enabled = true;
					button4.Enabled = true;
				}
				else
				{
					logged = true;
					webBrowser1.Visible = false;
					logger.Log("Program", "Shell started");
					logger.Log("Shell", "Successfully logged");
				}

				courses.Clear();

				HtmlElementCollection links = webBrowser1.Document.GetElementsByTagName("span");
				HtmlElementCollection found = null;

				foreach (HtmlElement link in links)
				{
					if (link.InnerText != "Мои курсы") continue;
					found = link.Parent.Parent.Children[1].Children;
					break;
				}
				foreach (HtmlElement link in found)
				{
					courses.Add(link);
					comboBox1.Items.Add(link.Children[0].Children[0].InnerText);
				}
			}
			else if (Utils.Match(url, "https://exam1.urfu.ru/mod/quiz/view.php"))
			{
				if (solve)
				{
					if (wholeSection && !course.Contains(comboBox2.Text, comboBox3.Text))
					{
						if (comboBox3.SelectedIndex == comboBox3.Items.Count - 1)
						{
							webBrowser1.Navigate("https://exam1.urfu.ru/");
						}
						else
						{
							comboBox3.SelectedIndex++;
							webBrowser1.Navigate(sectionLinks[comboBox3.SelectedIndex - 1]);
						}
						logger.Log(comboBox3.SelectedIndex, "💡 Not solved. The topic isn't in the database");
						return;
					}

					HtmlElementCollection links = webBrowser1.Document.GetElementsByTagName("div");

					foreach (HtmlElement link in links)
					{
						if (link.GetAttribute("className") != "singlebutton quizstartbuttondiv") continue;
						link.Children[0].Children[2].InvokeMember("click");
						return;
					}
				}
				else
				{
					HtmlElementCollection links = webBrowser1.Document.GetElementsByTagName("td");

					foreach (HtmlElement link in links)
					{
						if (link.GetAttribute("className") != "cell c4 lastcol") continue;
						webBrowser1.Navigate(link.Children[0].GetAttribute("href"));
						return;
					}
					logger.Log(comboBox3.SelectedIndex, "💡 Not saved. The topic isn't completed");

					if (wholeSection)
					{
						if (comboBox3.SelectedIndex == comboBox3.Items.Count - 1)
						{
							saveFileDialog1.FileName = comboBox2.Text;

							if (saveFileDialog1.ShowDialog() != DialogResult.Cancel)
							{
								course.Name = comboBox1.Text;
								File.WriteAllText(saveFileDialog1.FileName, JsonConvert.SerializeObject(course, Formatting.Indented));
							}

							webBrowser1.Navigate("https://exam1.urfu.ru/");
						}
						else
						{
							comboBox3.SelectedIndex++;
							webBrowser1.Navigate(sectionLinks[comboBox3.SelectedIndex - 1]);
						}
					}
					else
					{
						webBrowser1.Navigate("https://exam1.urfu.ru/");
					}
				}
			}
			else if (Utils.Match(url, "https://exam1.urfu.ru/mod/quiz/review.php"))
			{
				HtmlElementCollection links = webBrowser1.Document.GetElementsByTagName("form");
				HtmlElement found = null;

				foreach (HtmlElement link in links)
				{
					if (link.GetAttribute("className") != "questionflagsaveform") continue;
					found = link.Children[0];
					break;
				}

				foreach (HtmlElement link in found.Children)
				{
					if (link.Id == null) continue;

					string taskId = Utils.GetId(link.Id);
					string className = link.GetAttribute("className");

					if (Utils.Match(className, "que match deferredfeedback"))
					{
						List<string> answers;

						string text;

						var childs = link.Children[1].Children[1].Children[1].Children;

						int count = link.Children[1].Children[0].Children[3].Children[0].Children[0].Children.Count;

						if (childs.Count > 2 && childs[2].GetAttribute("className") == "rightanswer")
						{
							text = childs[2].InnerText.Substring(18);
						}
						else
						{
							text = childs[1].InnerText.Substring(18);
						}

						answers = Regex.Split(text, @", (?=\d+\.)").ToList();

						if (answers.Count != count)
						{
							answers.Clear();
							answers = text.Split(',').ToList();

							for (int i = 1; i < answers.Count; i++)
							{
								if (answers[i].Contains("→")) continue;
								answers[i - 1] += "," + answers[i];
								answers.RemoveAt(i);
								i--;
							}
						}

						if (answers.Count != count)
						{
							answers.Clear();
							answers = text.Split(new string[] { ".,", "!,", "?," }, StringSplitOptions.None).ToList();
						}

						if (answers.Count != count)
						{
							answers.Clear();
							text = text.Substring(2);
							answers = text.Split(new string[] { "\r\n\r\n" }, StringSplitOptions.None).ToList();
						}

						if (answers.Count != count)
						{
							throw new Exception();
						}

						var _answers = answers.Select(x => x.Split('→')).ToDictionary(x => x[0].Trim(), y => y[1].Trim());

						course.Put(comboBox2.Text, comboBox3.Text, taskId, _answers);
					}
					else if (Utils.Match(className, "que multianswer deferredfeedback"))
					{
						var child = link.Children[1].Children[0];

						var _answers = new Dictionary<string, string>();

						HtmlElementCollection _childs = child.GetElementsByTagName("span");

						int count = 0;

						foreach (HtmlElement __child in _childs)
						{
							if (!Utils.Match(__child.GetAttribute("className"), "subquestion")) continue;

							string answer = __child.Children[3].InnerText;

							int startIdx = answer.IndexOf("Правильный ответ") + 18;
							int endIdx = answer.IndexOf("Баллов") - 2;

							if (endIdx == -3) endIdx = answer.Length;

							_answers.Add(count.ToString(), answer.Substring(startIdx, endIdx - startIdx));
							count++;
						}
						for (int i = 0; i < child.Children.Count; i++)
						{
							if (child.Children[i].TagName != "DIV" ||
								child.Children[i].GetAttribute("className") != "answer") continue;

							string key = child.Children[i - 1].InnerText == null ? child.Children[i - 2].InnerText : child.Children[i - 1].InnerText;

							_answers.Add(key, child.Children[i + 1].InnerText.Substring(42));
						}
						course.Put(comboBox2.Text, comboBox3.Text, taskId, _answers);
					}
					else if (Utils.Match(className, "que ddwtos deferredfeedback"))
					{
						var _answers = new Dictionary<string, string>();

						string text = link.Children[1].Children[1].Children[1].Children[1].InnerText;

						var matches = Regex.Matches(text, @"\[(.+?)\]");

						for (int i = 0; i < matches.Count; i++)
						{
							_answers.Add(i.ToString(), matches[i].Groups[1].Value.Trim());
						}
						course.Put(comboBox2.Text, comboBox3.Text, taskId, _answers);
					}
					else if (Utils.Match(className, "que gapselect deferredfeedback"))
					{
						var _answers = new Dictionary<string, string>();

						string text = link.Children[1].Children[1].Children[1].Children[1].InnerText;

						var answers = Regex.Matches(text, @"\[(.+?)\]");

						for (int i = 0; i < answers.Count; i++)
						{
							_answers.Add(i.ToString(), answers[i].Groups[1].Value);
						}
						course.Put(comboBox2.Text, comboBox3.Text, taskId, _answers);
					}
					else if (Utils.Match(className, "que multichoice deferredfeedback"))
					{
						var _answers = new Dictionary<string, string>();

						string[] answers = link.Children[1].Children[1].Children[1].Children[1].InnerText.Substring(19).Split(new string[] { ", " }, StringSplitOptions.None);
						
						for (int i = 0; i < answers.Length; i++)
						{
							_answers.Add(i.ToString(), answers[i]);
						}
						course.Put(comboBox2.Text, comboBox3.Text, taskId, _answers);
					}
				}
				logger.Log(comboBox3.SelectedIndex, "✅ Successfully saved");

				if (wholeSection)
				{
					if (comboBox3.SelectedIndex == comboBox3.Items.Count - 1)
					{
						saveFileDialog1.FileName = comboBox2.Text;

						if (saveFileDialog1.ShowDialog() != DialogResult.Cancel)
						{
							course.Name = comboBox1.Text;
							File.WriteAllText(saveFileDialog1.FileName, JsonConvert.SerializeObject(course, Formatting.Indented));
						}

						webBrowser1.Navigate("https://exam1.urfu.ru/");
					}
					else
					{
						comboBox3.SelectedIndex++;
						webBrowser1.Navigate(sectionLinks[comboBox3.SelectedIndex - 1]);
					}
				}
				else
				{
					saveFileDialog1.FileName = comboBox3.Text;

					if (saveFileDialog1.ShowDialog() != DialogResult.Cancel)
					{
						course.Name = comboBox1.Text;
						File.WriteAllText(saveFileDialog1.FileName, JsonConvert.SerializeObject(course, Formatting.Indented));
					}

					webBrowser1.Navigate("https://exam1.urfu.ru/");
				}
			}
			else if (Utils.Match(url, "https://exam1.urfu.ru/mod/quiz/attempt.php"))
			{
				HtmlElement found = webBrowser1.Document.GetElementById("responseform").Children[0];
				HtmlElement task = found.Children[0];

				string taskId = Utils.GetId(task.Id);
				string className = task.GetAttribute("className");

				if (Utils.Match(className, "que match deferredfeedback"))
				{
					var childs = task.Children[1].Children[0].Children[3].Children[0].Children[0].Children;

					foreach (HtmlElement child in childs)
					{
						string key;

						if (child.Children[0].Children.Count == 0)
						{
							key = child.Children[0].InnerText;
						}
						else
						{
							key = child.Children[0].Children[0].InnerText;
						}

						var options = child.Children[1].Children[1].Children;

						string answer;

						if (course.Sections[comboBox2.Text][comboBox3.Text][taskId].ContainsKey(key))
							answer = course.Sections[comboBox2.Text][comboBox3.Text][taskId][key];
						else
						{
							answer = course.Sections[comboBox2.Text][comboBox3.Text][taskId]
								.First(x => key.Contains(x.Key)).Value;
						}

						foreach (HtmlElement option in options)
						{
							if (!Utils.Match(option.InnerText, answer)) continue;
							option.SetAttribute("selected", "selected");
						}
					}
				}
				else if (Utils.Match(className, "que multianswer deferredfeedback"))
				{
					var child = task.Children[1].Children[0];

					string[] answers = course.Sections[comboBox2.Text][comboBox3.Text][taskId].Where(x => int.TryParse(x.Key, out _)).OrderBy(x => int.Parse(x.Key)).Select(x => x.Value).ToArray();

					HtmlElementCollection _childs = child.GetElementsByTagName("span");

					int count = 0;

					foreach (HtmlElement __child in _childs)
					{
						string _className = __child.GetAttribute("className");
						if (_className == "subquestion form-inline d-inline")
						{
							__child.Children[1].SetAttribute("value", answers[count]);
							count++;
						}
						else if (_className == "subquestion")
						{
							var options = __child.Children[1].Children;

							foreach (HtmlElement option in options)
							{
								if (option.InnerText != answers[count]) continue;
								option.SetAttribute("selected", "selected");
								count++;
								break;
							}
						}
					}
					for (int i = 0; i < child.Children.Count; i++)
					{
						if (child.Children[i].TagName != "DIV" ||
							child.Children[i].GetAttribute("className") != "answer") continue;

						string key = child.Children[i - 1].InnerText == null ? child.Children[i - 2].InnerText : child.Children[i - 1].InnerText;

						var options = child.Children[i].Children;

						string answer = course.Sections[comboBox2.Text][comboBox3.Text][taskId][key];

						foreach (HtmlElement option in options)
						{
							if (option.Children[1].InnerText != answer) continue;
							option.Children[0].InvokeMember("click");
						}
					}
				}
				else if (Utils.Match(className, "que ddwtos deferredfeedback"))
				{
					string[] answers = course.Sections[comboBox2.Text][comboBox3.Text][taskId].OrderBy(x => int.Parse(x.Key)).Select(x => x.Value).ToArray();

					var fields = task.Children[1].Children[0];
					var child = fields.Children[3];

					int count = child.Children.Count;

					Dictionary<string, string>[] cards = new Dictionary<string, string>[count];

					for (int i = 0; i < count; i++)
					{
						cards[i] = new Dictionary<string, string>();
						for (int q = 0; q < child.Children[i].Children.Count; q++)
						{
							cards[i].Add(child.Children[i].Children[q].InnerText.Trim(), (q + 1).ToString());
						}
					}

					for (int i = 4; i < fields.Children.Count; i++)
					{
						fields.Children[i].SetAttribute("value", cards[i % count][answers[i - 4]]);
					}
				}
				else if (Utils.Match(className, "que gapselect deferredfeedback"))
				{
					string[] answers = course.Sections[comboBox2.Text][comboBox3.Text][taskId].OrderBy(x => int.Parse(x.Key)).Select(x => x.Value).Select(x => x.Replace('‑', '-')).ToArray();

					var child = task.Children[1].Children[0].Children[2];

					HtmlElementCollection _childs = child.GetElementsByTagName("span");

					int count = 0;

					foreach (HtmlElement __child in _childs)
					{
						if (__child.GetAttribute("className") != "control group1") continue;

						var options = __child.Children[0].Children;

						foreach (HtmlElement option in options)
						{
							if (option.InnerText != answers[count]) continue;
							option.SetAttribute("selected", "selected");
							count++;
							break;
						}
					}
				}
				else if (Utils.Match(className, "que multichoice deferredfeedback"))
				{
					string[] answers = course.Sections[comboBox2.Text][comboBox3.Text][taskId].Select(x => x.Value).ToArray();

					var gap = task.Children[1].Children[0].Children[3].Children[1];

					foreach (HtmlElement child in gap.Children)
					{
						if (!answers.Contains(child.Children[2].Children[0].Children[0].InnerText)) continue;
						child.Children[1].SetAttribute("checked", "checked");
					}
				}
				if (found.Children[1].Children.Count < 2) found.Children[1].Children[0].InvokeMember("click");
				else found.Children[1].Children[1].InvokeMember("click");
			}
			else if (Utils.Match(url, "https://exam1.urfu.ru/mod/quiz/summary.php"))
			{
				HtmlElement el = webBrowser1.Document.GetElementById("region-main");
				string text = el.InnerText;
				if (!text.Contains("Пока нет ответа") && !text.Contains("Неполный ответ"))
				{
					HtmlElementCollection links = webBrowser1.Document.GetElementsByTagName("button");

					foreach (HtmlElement link in links)
					{
						if (link.InnerText != "Отправить всё и завершить тест") continue;
						link.InvokeMember("click");
						break;
					}
					logger.Log(comboBox3.SelectedIndex, "✅ Successfully solved");
				}
				else
				{
					logger.Log(comboBox3.SelectedIndex, "✅ Partially solved. Not sent");
				}

				if (wholeSection)
				{
					if (comboBox3.SelectedIndex == comboBox3.Items.Count - 1)
					{
						webBrowser1.Navigate("https://exam1.urfu.ru/");
					}
					else
					{
						comboBox3.SelectedIndex++;
						webBrowser1.Navigate(sectionLinks[comboBox3.SelectedIndex - 1]);
					}
				}
				else
				{
					webBrowser1.Navigate("https://exam1.urfu.ru/");
				}
			}
		}

		private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (comboBox1.SelectedIndex == 0) return;

			int idx = comboBox1.SelectedIndex - 1;

			sections.Clear();
			comboBox2.Items.Clear();
			comboBox2.Enabled = false;

			comboBox2.Items.Add("Выберите раздел");

			if (courses[idx].Children.Count == 1)
			{
				courses[idx].Children[0].InvokeMember("click");
				while (courses[idx].Children.Count == 1) Application.DoEvents();
			}

			foreach (HtmlElement link in courses[idx].Children[1].Children)
			{
				string text = link.Children[0].Children[0].Children[0].InnerText;
				if (text == null) continue;

				sections.Add(link);
				comboBox2.Items.Add(text);
			}
			comboBox2.SelectedIndex = 0;
			comboBox2.Enabled = true;
		}

		private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (comboBox2.SelectedIndex == 0) return;

			int idx = comboBox2.SelectedIndex - 1;

			topics.Clear();
			comboBox3.Items.Clear();
			comboBox3.Enabled = false;

			comboBox3.Items.Add("Выберите тему");

			if (sections[idx].Children.Count == 1)
			{
				sections[idx].Children[0].InvokeMember("click");
				while (sections[idx].Children.Count == 1) Application.DoEvents();
			}

			foreach (HtmlElement link in sections[idx].Children[1].Children)
			{
				topics.Add(link);
				comboBox3.Items.Add(link.Children[0].Children[0].InnerText);
			}
			comboBox3.SelectedIndex = 0;
			comboBox3.Enabled = true;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			comboBox1.Enabled = false;
			comboBox2.Enabled = false;
			comboBox3.Enabled = false;
			button1.Enabled = false;
			button2.Enabled = false;
			button3.Enabled = false;
			button4.Enabled = false;

			int idx = comboBox3.SelectedIndex - 1;

			string url = topics[idx].Children[0].Children[0].GetAttribute("href");

			webBrowser1.Navigate(url);
		}

		private void button2_Click(object sender, EventArgs e)
		{
			if (!course.Contains(comboBox2.Text, comboBox3.Text))
			{
				if (openFileDialog1.ShowDialog() == DialogResult.Cancel) return;

				course = JsonConvert.DeserializeObject<Course>(File.ReadAllText(openFileDialog1.FileName));

				if (!course.Contains(comboBox2.Text, comboBox3.Text))
				{
					MessageBox.Show("Этого теста нет в данной базе", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
			}

			comboBox1.Enabled = false;
			comboBox2.Enabled = false;
			comboBox3.Enabled = false;
			button1.Enabled = false;
			button2.Enabled = false;
			button3.Enabled = false;
			button4.Enabled = false;

			solve = true;

			int idx = comboBox3.SelectedIndex - 1;

			webBrowser1.Navigate(topics[idx].Children[0].Children[0].GetAttribute("href"));
		}

		private void button3_Click(object sender, EventArgs e)
		{
			comboBox1.Enabled = false;
			comboBox2.Enabled = false;
			comboBox3.Enabled = false;
			button1.Enabled = false;
			button2.Enabled = false;
			button3.Enabled = false;
			button4.Enabled = false;

			wholeSection = true;

			sectionLinks.Clear();
			foreach (HtmlElement topic in topics)
			{
				sectionLinks.Add(topic.Children[0].Children[0].GetAttribute("href"));
			}

			comboBox3.SelectedIndex = 1;

			webBrowser1.Navigate(sectionLinks[0]);
		}

		private void button4_Click(object sender, EventArgs e)
		{
			if (!course.Contains(comboBox2.Text))
			{
				if (openFileDialog1.ShowDialog() == DialogResult.Cancel) return;

				course = JsonConvert.DeserializeObject<Course>(File.ReadAllText(openFileDialog1.FileName));

				if (!course.Contains(comboBox2.Text))
				{
					MessageBox.Show("Этого раздела нет в данной базе", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
			}

			comboBox1.Enabled = false;
			comboBox2.Enabled = false;
			comboBox3.Enabled = false;
			button1.Enabled = false;
			button2.Enabled = false;
			button3.Enabled = false;
			button4.Enabled = false;

			solve = true;
			wholeSection = true;

			sectionLinks.Clear();
			foreach (HtmlElement topic in topics)
			{
				sectionLinks.Add(topic.Children[0].Children[0].GetAttribute("href"));
			}

			comboBox3.SelectedIndex = 1;

			webBrowser1.Navigate(sectionLinks[0]);
		}
	}
}
