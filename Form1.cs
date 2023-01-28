using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ExamSolver
{
	public partial class Form1 : Form
	{
		bool logged = false;
		bool solve = false;
		bool wholeSection = false;
		bool warning = false;
		bool firstTime = false;

		Course course = new Course();
		Logger logger;

		List<HtmlElement> courses = new List<HtmlElement>();
		List<HtmlElement> sections = new List<HtmlElement>();
		List<HtmlElement> topics = new List<HtmlElement>();

		List<string> sectionLinks = new List<string>();

		Random rand = new Random();

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

					checkBox1.Enabled = true;

					comboBox1.Items.Clear();
					comboBox1.Items.Add("Выберите курс");

					comboBox1.SelectedIndex = 0;
					comboBox2.SelectedIndex = 0;
					comboBox3.SelectedIndex = 0;
					numericUpDown1.Value = 1;
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
				comboBox1.Enabled = true;
			}
			else if (Utils.Match(url, "https://exam1.urfu.ru/mod/quiz/view.php"))
			{
				if (solve)
				{
					if (wholeSection && !course.Contains(comboBox2.Text, comboBox3.Text))
					{
						logger.Log(comboBox3.SelectedIndex, "💡 Not solved. This topic isn't in the database");
						label1.Text = "Test: " + comboBox3.SelectedIndex + "/" + (comboBox3.Items.Count - 1);

						if (comboBox3.SelectedIndex == comboBox3.Items.Count - 1)
						{
							webBrowser1.Navigate("https://exam1.urfu.ru/");
						}
						else
						{
							comboBox3.SelectedIndex++;
							webBrowser1.Navigate(sectionLinks[comboBox3.SelectedIndex - 1]);
						}
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
					logger.Log(comboBox3.SelectedIndex, "💡 Not saved. This topic isn't completed");
					label1.Text = "Test: " + comboBox3.SelectedIndex + "/" + (comboBox3.Items.Count - 1);

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
				HtmlElementCollection found = null;

				foreach (HtmlElement link in links)
				{
					if (link.GetAttribute("className") != "questionflagsaveform") continue;
					found = link.Children[0].Children;
					break;
				}

				foreach (HtmlElement link in found)
				{
					if (link.Id == null) continue;

					string taskId = Utils.GetId(link.Id);
					string taskType = link.GetAttribute("className");

					if (Utils.Match(taskType, "que match deferredfeedback"))
					{
						List<string> answers;

						var childs = link.Children[1].Children[0].Children;

						string text;

						int count;

						if (childs.Count > 3)
						{
							count = childs[3].Children[0].Children[0].Children.Count;
						}
						else
						{
							count = childs[1].Children[2].Children[0].Children[0].Children.Count;
						}

						childs = link.Children[1].Children[1].Children[1].Children;

						if (childs.Count > 2)
						{
							text = childs[2].InnerText.Substring(18);
						}
						else if (childs.Count > 1)
						{
							text = childs[1].InnerText.Substring(18);
						}
						else continue;

						answers = text.Split(new string[] { ".,", "!,", "?," }, StringSplitOptions.None).ToList();

						if (answers.Count != count)
						{
							answers = Regex.Split(text, @", (?=\d+\.)").ToList();
						}

						if (answers.Count != count)
						{
							answers = text.Split(new string[] { ", \r\n\r\n" }, StringSplitOptions.None).ToList();
						}

						if (answers.Count != count)
						{
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
							answers = text.Split(',').ToList();

							for (int i = 1; i < answers.Count; i++)
							{
								if (answers[i - 1].Contains("→")) continue;
								answers[i - 1] += "," + answers[i];
								answers.RemoveAt(i);
								i--;
							}
						}

						if (answers.Count != count)
						{
							throw new Exception();
						}

						var _answers = answers.Select(x => x.Split('→')).ToDictionary(x => x[0].Trim(), y => y[1].Trim());

						course.Put(comboBox2.Text, comboBox3.Text, taskId, _answers);
					}
					else if (Utils.Match(taskType, "que multianswer deferredfeedback"))
					{
						var answers = new Dictionary<string, string>();

						var root = link.Children[1].Children[0];

						int count = 0;

						HtmlElementCollection childs = root.GetElementsByTagName("span");

						foreach (HtmlElement child in childs)
						{
							if (!Utils.Match(child.GetAttribute("className"), "subquestion")) continue;

							string answer = child.Children[3].InnerText;

							int startIdx = answer.IndexOf("Правильный ответ") + 18;
							int endIdx = answer.IndexOf("Баллов") - 2;

							if (endIdx == -3) endIdx = answer.Length;

							if (startIdx > endIdx) break;

							answers.Add(count.ToString(), answer.Substring(startIdx, endIdx - startIdx));
							count++;
						}

						childs = root.GetElementsByTagName("div");

						foreach (HtmlElement child in childs)
						{
							if (child.GetAttribute("className") != "outcome") continue;

							string answer = child.InnerText;

							int startIdx = answer.IndexOf("Правильный ответ") + 18;

							answers.Add(count.ToString(), answer.Substring(startIdx));
							count++;
						}
						course.Put(comboBox2.Text, comboBox3.Text, taskId, answers);
					}
					else if (Utils.Match(taskType, "que ddwtos deferredfeedback")
						|| Utils.Match(taskType, "que gapselect deferredfeedback"))
					{
						var answers = new Dictionary<string, string>();

						var childs = link.Children[1].Children[1].Children[1].Children;

						string text;

						if (childs.Count > 2)
						{
							text = childs[2].InnerText;
						}
						else if (childs.Count > 1)
						{
							text = childs[1].InnerText;
						}
						else continue;

						var matches = Regex.Matches(text, @"\[(.+?)\]");

						for (int i = 0; i < matches.Count; i++)
						{
							answers.Add(i.ToString(), matches[i].Groups[1].Value.Trim());
						}
						course.Put(comboBox2.Text, comboBox3.Text, taskId, answers);
					}
					else if (Utils.Match(taskType, "que multichoice deferredfeedback"))
					{
						var _answers = new Dictionary<string, string>();

						var childs = link.Children[1].Children[1].Children[1].Children;

						string[] answers;

						if (childs.Count > 2)
						{
							answers = childs[2].InnerText.Substring(19).Split(',');
						}
						else if (childs.Count > 1)
						{
							answers = childs[1].InnerText.Substring(19).Split(',');
						}
						else continue;

						for (int i = 0; i < answers.Length; i++)
						{
							_answers.Add(i.ToString(), answers[i].Trim());
						}
						course.Put(comboBox2.Text, comboBox3.Text, taskId, _answers);
					}
				}
				logger.Log(comboBox3.SelectedIndex, "✅ Successfully saved");
				label1.Text = "Test: " + comboBox3.SelectedIndex + "/" + (comboBox3.Items.Count - 1);

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
				string taskType = task.GetAttribute("className");

				if (checkBox1.Checked && !firstTime)
				{
					Thread.Sleep(rand.Next((int)numericUpDown2.Value * 60000, ((int)numericUpDown2.Value + 1) * 60000));
					firstTime = true;
				}

				if (Utils.Match(taskType, "que match deferredfeedback"))
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
							break;
						}
					}
				}
				else if (Utils.Match(taskType, "que multianswer deferredfeedback"))
				{
					string[] answers = course.Sections[comboBox2.Text][comboBox3.Text][taskId].OrderBy(x => int.Parse(x.Key)).Select(x => x.Value).ToArray();

					var root = task.Children[1].Children[0];

					int count = 0;

					HtmlElementCollection childs = root.GetElementsByTagName("span");

					foreach (HtmlElement child in childs)
					{
						string keyType = child.GetAttribute("className");
						if (keyType == "subquestion form-inline d-inline")
						{
							child.Children[1].SetAttribute("value", answers[count]);
							count++;
						}
						else if (keyType == "subquestion")
						{
							var options = child.Children[1].Children;

							foreach (HtmlElement option in options)
							{
								if (option.InnerText != answers[count]) continue;
								option.SetAttribute("selected", "selected");
								count++;
								break;
							}
						}
					}

					childs = root.All;

					foreach (HtmlElement child in childs)
					{
						if (child.GetAttribute("className") != "answer") continue;

						var options = child.Children.Count > 1 ? child.Children : child.Children[0].Children[0].Children;

						foreach (HtmlElement option in options)
						{
							if (option.Children[1].InnerText != answers[count]) continue;
							option.Children[0].InvokeMember("click");
							count++;
							break;
						}
					}
				}
				else if (Utils.Match(taskType, "que ddwtos deferredfeedback"))
				{
					string[] answers = course.Sections[comboBox2.Text][comboBox3.Text][taskId].OrderBy(x => int.Parse(x.Key)).Select(x => x.Value).ToArray();

					var fields = task.Children[1].Children[0];
					var root = fields.Children[3];

					int count = root.Children.Count;

					Dictionary<string, string>[] cards = new Dictionary<string, string>[count];

					for (int i = 0; i < count; i++)
					{
						cards[i] = new Dictionary<string, string>();
						for (int q = 0; q < root.Children[i].Children.Count; q++)
						{
							cards[i].Add(root.Children[i].Children[q].InnerText.Trim(), (q + 1).ToString());
						}
					}
					for (int i = 4; i < fields.Children.Count; i++)
					{
						fields.Children[i].SetAttribute("value", cards[i % count][answers[i - 4]]);
					}
				}
				else if (Utils.Match(taskType, "que gapselect deferredfeedback"))
				{
					string[] answers = course.Sections[comboBox2.Text][comboBox3.Text][taskId].OrderBy(x => int.Parse(x.Key)).Select(x => x.Value).Select(x => x.Replace('‑', '-')).ToArray();

					var root = task.Children[1].Children[0].Children[2];

					int count = 0;

					HtmlElementCollection childs = root.GetElementsByTagName("span");

					foreach (HtmlElement child in childs)
					{
						if (child.GetAttribute("className") != "control group1") continue;

						var options = child.Children[0].Children;

						foreach (HtmlElement option in options)
						{
							if (option.InnerText != answers[count]) continue;
							option.SetAttribute("selected", "selected");
							count++;
							break;
						}
					}
				}
				else if (Utils.Match(taskType, "que multichoice deferredfeedback"))
				{
					string[] answers = course.Sections[comboBox2.Text][comboBox3.Text][taskId].Select(x => x.Value).ToArray();

					var childs = task.Children[1].Children[0].Children[3].Children[1].Children;

					foreach (HtmlElement child in childs)
					{
						if (!answers.Contains(child.Children[2].Children[0].Children[0].InnerText)) continue;
						child.Children[1].SetAttribute("checked", "checked");
					}
				}
				if (found.Children[1].Children.Count > 1) found.Children[1].Children[1].InvokeMember("click");
				else found.Children[1].Children[0].InvokeMember("click");
			}
			else if (Utils.Match(url, "https://exam1.urfu.ru/mod/quiz/summary.php"))
			{
				string text = webBrowser1.Document.GetElementById("region-main").InnerText;

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
				firstTime = false;
				label1.Text = "Test: " + comboBox3.SelectedIndex + "/" + (comboBox3.Items.Count - 1);

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
			comboBox2.Enabled = false;

			sections.Clear();
			comboBox2.Items.Clear();
			comboBox3.SelectedIndex = 0;

			comboBox2.Items.Add("Выберите раздел");
			comboBox2.SelectedIndex = 0;

			if (comboBox1.SelectedIndex == 0) return;

			int idx = comboBox1.SelectedIndex - 1;

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
			comboBox2.Enabled = true;
		}

		private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
		{
			comboBox3.Enabled = false;
			label2.Enabled = false;
			numericUpDown1.Enabled = false;

			topics.Clear();
			comboBox3.Items.Clear();

			comboBox3.Items.Add("Выберите тему");
			comboBox3.SelectedIndex = 0;

			if (comboBox2.SelectedIndex == 0)
			{
				button3.Enabled = false;
				button4.Enabled = false;
				return;
			}
			button3.Enabled = true;
			button4.Enabled = true;

			int idx = comboBox2.SelectedIndex - 1;

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
			comboBox3.Enabled = true;
			label2.Enabled = true;
			numericUpDown1.Enabled = true;
			numericUpDown1.Maximum = comboBox3.Items.Count - 1;
		}

		private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!comboBox3.Enabled) return;

			if (comboBox3.SelectedIndex == 0)
			{
				button1.Enabled = false;
				button2.Enabled = false;
				return;
			}
			button1.Enabled = true;
			button2.Enabled = true;
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
			label2.Enabled = false;
			numericUpDown1.Enabled = false;
			checkBox1.Enabled = false;
			numericUpDown2.Enabled = false;

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
			label2.Enabled = false;
			numericUpDown1.Enabled = false;
			checkBox1.Enabled = false;
			numericUpDown2.Enabled = false;

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
			label2.Enabled = false;
			numericUpDown1.Enabled = false;
			checkBox1.Enabled = false;
			numericUpDown2.Enabled = false;

			wholeSection = true;

			sectionLinks.Clear();
			foreach (HtmlElement topic in topics)
			{
				sectionLinks.Add(topic.Children[0].Children[0].GetAttribute("href"));
			}

			comboBox3.SelectedIndex = 1;

			webBrowser1.Navigate(sectionLinks[0]);

			label1.Text = "Test: 0/" + (comboBox3.Items.Count - 1);
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
			label2.Enabled = false;
			numericUpDown1.Enabled = false;
			checkBox1.Enabled = false;
			numericUpDown2.Enabled = false;

			solve = true;
			wholeSection = true;

			sectionLinks.Clear();
			foreach (HtmlElement topic in topics)
			{
				sectionLinks.Add(topic.Children[0].Children[0].GetAttribute("href"));
			}

			comboBox3.SelectedIndex = (int)numericUpDown1.Value;

			webBrowser1.Navigate(sectionLinks[(int)numericUpDown1.Value - 1]);

			label1.Text = "Test: 0/" + (comboBox3.Items.Count - 1);
		}

		private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start("https://vk.com/id217626683");
		}

		private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start("https://vk.com/id253591799");
		}

		private void checkBox1_CheckedChanged(object sender, EventArgs e)
		{
			numericUpDown2.Enabled = checkBox1.Checked;

			if (warning || !checkBox1.Checked) return;

			if (MessageBox.Show("Интервал работает, но происходит посредством замораживания процесса.\nВо время заморозки окно не будет отвечать на команды пользователя.", "ЭКСПЕРИМЕНТАЛЬНАЯ ФИЧА", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)
			{
				checkBox1.Checked = false;
				return;
			}
			warning = true;
		}
	}
}
