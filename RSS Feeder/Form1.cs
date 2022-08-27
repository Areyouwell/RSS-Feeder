using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace RSS_Feeder
{
    public partial class Form1 : Form
    {
        static int alarmCounter = 1; // Удалить это мусор
        private string? _frequencyType = "Hour";
        private int _frequencyValue = 5000;
        private string _defaultFeed = "https://habr.com/rss/interesting/";
        private List<string> _allFeed = new List<string>();
        private List<TabPage> _allTabPages = new List<TabPage>();
        private string _proxyAddress = "";
        private string _userName = "";
        private string _password = "";
        DateFromXml inputXML = new DateFromXml();

        private enum TimeSlotType
        {
            Minute,
            Hour
        }

        public Form1()
        {
            InitializeComponent();

            InitialParametr();

            timer1.Tick += new EventHandler(TimerEventProcessor!);
            timer1.Interval = _frequencyValue;
            timer1.Start();
        }

        public void BuildTape(List<Item> items, string tabPageName = "tabPage1")
        {
            int size = 0;
            Panel panel1 = new Panel();
            panel1.Location = new Point(10, 10);
            panel1.Size = new Size(400, 375);
            panel1.AutoScroll = true;
            panel1.BackColor = Color.Silver;
            panel1.Anchor = AnchorStyles.Bottom | AnchorStyles.Top;

            if (tabPageName != "tabPage1")
            {
                TabPage tabPageV = _allTabPages.FindLast(x => x.Text == tabPageName)!;
                tabPageV.Controls.Clear();
                tabPageV.Controls.Add(panel1);
            }
            else
            {
                tabPage1.Controls.Clear();
                tabPage1.Controls.Add(panel1);
            }

            foreach (var item in items)
            {
                GroupBox gb = new GroupBox();
                gb.Location = new Point(5, 5 + size);
                gb.Size = new Size(370, 250);
                panel1.Controls.Add(gb);
                size = size + 250;

                LinkLabel linkLabel1 = new LinkLabel();
                linkLabel1.LinkClicked += (object sender, LinkLabelLinkClickedEventArgs e) 
                    => System.Diagnostics.Process.Start(new ProcessStartInfo
                    { FileName = item.Link, UseShellExecute = true });
                linkLabel1.Location = new Point(10, 10);
                linkLabel1.Size = new Size(350, 75);
                linkLabel1.Text = item.Title;
                gb.Controls.Add(linkLabel1);

                Label lb1 = new Label();
                lb1.Location = new Point(10, 90);
                lb1.Size = new Size(350, 20);
                lb1.Text = "Publication date:  " + DateTime.Parse(item.PubDate!).ToShortDateString() 
                    + "     " + DateTime.Parse(item.PubDate!).ToShortTimeString();
                gb.Controls.Add(lb1);

                Label lb2 = new Label();
                lb2.Location = new Point(10, 110);
                lb2.Size = new Size(350, 20);
                lb2.Text = "Description";
                gb.Controls.Add(lb2);

                TextBox tb1 = new TextBox();
                tb1.Location = new Point(10, 130);
                tb1.Size = new Size(350, 110);
                tb1.Multiline = true;
                tb1.ScrollBars = ScrollBars.Vertical;
                tb1.Text = item.Description;
                gb.Controls.Add(tb1);
            }
        } 
        
        public async Task<List<Item>> GetXmlAsync(string feed)
        {
            WebProxy wp = new WebProxy(_proxyAddress);
            wp.Credentials = new NetworkCredential(_userName, _password);
            HttpClientHandler httpClientHandler = new HttpClientHandler() { Proxy = wp, UseProxy = true };

            HttpClient client = new HttpClient(httpClientHandler);
            var response = await client.GetStringAsync(feed);

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(response);

            XDocument xdoc = xmlDoc.ToXDocument();
            XElement? channelXML = xdoc.Element("rss")?.Element("channel");

            List<Item> items = FindElement(channelXML!);

            return items;
        }

        public async Task ReadXMLAsync()
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(DateFromXml));
            await using (FileStream fs = new FileStream("XMLFile1.xml", FileMode.OpenOrCreate))
            {
                inputXML = (xmlSerializer.Deserialize(fs) as DateFromXml)!;
            }
            if(inputXML.Tape is not null)
                _defaultFeed = inputXML.Tape;
            _frequencyValue = inputXML.Frequency;
            _proxyAddress = inputXML.ProxyAddress!;
            _userName = inputXML.UserName!;
            _password = inputXML.Password!;
        }

        public List<Item> FindElement(XElement channelXML)
        {
            List<Item> items = new List<Item>();
            if (channelXML is not null)
            {
                foreach (XElement itemXML in channelXML.Elements("item"))
                {
                    Item item = new Item();
                    item.Title = ScrubHtml(itemXML.Element("title")?.Value!);
                    item.Link = itemXML.Element("link")?.Value;
                    item.Description = ScrubHtml(itemXML.Element("description")?.Value!);
                    item.PubDate = itemXML.Element("pubDate")?.Value;
                    items.Add(item);
                }
            }
            return items;
        }

        public static string ScrubHtml(string value)
        {
            var step1 = Regex.Replace(value, @"<[^>]+>|&nbsp;", "").Trim();
            var step2 = Regex.Replace(step1, @"\s{2,}", " ");
            var step3 = step2.Replace("&rarr;", ".");
            return step3;
        }

        private async void TimerEventProcessor(Object myObject, EventArgs myEventArgs)
        {
            label15.Text = "Число:" + alarmCounter.ToString(); // удалть мусор
            label12.Text = "Тип  " + _frequencyType + "   Знаечние  " + _frequencyValue.ToString(); // удалить мусор
            alarmCounter += 1; // удалить мусор
            timer1.Interval = _frequencyValue;
            List<Item> itemsDef = await GetXmlAsync(_defaultFeed);
            BuildTape(itemsDef);
            foreach (var oneFeed in _allFeed)
            {
                List<Item> items = await GetXmlAsync(oneFeed);
                BuildTape(items, oneFeed);
            }
        }

        public async void InitialParametr()
        {
            StartPosition = FormStartPosition.CenterScreen;
            comboBox1.Enabled = false;
            numericUpDown1.Enabled = false;
            radioButton1.Checked = true;
            button2.Enabled = false;
            comboBox1.Items.AddRange(Enum.GetNames(typeof(TimeSlotType)));

            await ReadXMLAsync();
            List<Item> items = await GetXmlAsync(_defaultFeed);
            BuildTape(items);
        }

        public class DateFromXml
        {
            public string? Tape { get; set; }
            public int Frequency { get; set; }
            public string? ProxyAddress { get; set; }
            public string? UserName { get; set; }
            public string? Password { get; set; }
        }

        public class Item
        {
            public string? Title { get; set; }
            public string? Link { get; set; }
            public string? Description { get; set; }
            public string? PubDate { get; set; }
        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        { 

        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem != null)
            {
                _frequencyType = comboBox1.SelectedItem.ToString();
            }
            switch (_frequencyType)
            {
                case "Minute":
                    _frequencyValue = (int)numericUpDown1.Value * 60 * 1000;
                    break;
                case "Hour":
                    _frequencyValue = (int)numericUpDown1.Value * 60 * 60 * 1000;
                    break;
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            comboBox1.Enabled = true;
            numericUpDown1.Enabled = true;
            button2.Enabled = true;
            comboBox1.SelectedItem = comboBox1.Items[1];
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            comboBox1.Enabled = false;
            numericUpDown1.Enabled = false;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var cbSelected = comboBox2.SelectedItem;
            if (cbSelected != null)
            {
                _allFeed.Remove(cbSelected.ToString()!);
                comboBox2.Items.Remove(cbSelected);
                tabControl1.Controls.Remove(_allTabPages.Find(x => x.Text == cbSelected.ToString()));
                _allTabPages.Remove(_allTabPages.Find(x => x.Text == cbSelected.ToString())!);
            }
            else
                MessageBox.Show("Select feed");
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            if (textBox1.Text != "" && textBox1.Text != null)
            {
                _allFeed.Add(textBox1.Text);
                comboBox2.Items.Add(textBox1.Text);
                TabPage tabPage = new TabPage();
                tabPage.Text = textBox1.Text;
                _allTabPages.Add(tabPage);
                tabControl1.Controls.Add(tabPage);
                List<Item> items = await GetXmlAsync(textBox1.Text);
                BuildTape(items, tabPage.Text);
                textBox1.Clear();
            }
            else
                MessageBox.Show("Enter link");
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }

    public static class DocumentExtensions
    { 
        public static XDocument ToXDocument(this XmlDocument xmlDocument)
        {
            using (var nodeReader = new XmlNodeReader(xmlDocument))
            {
                nodeReader.MoveToContent();
                return XDocument.Load(nodeReader);
            }
        }
    }
}