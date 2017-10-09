using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CSVCompare
{
    public partial class Form1 : Form
    {
        private Dictionary<string, string> master = new Dictionary<string, string>();
        private Dictionary<string, string> slave = new Dictionary<string, string>();

        private const string labelCol = "event_label";
        private const string locCol = "event_location";

        private int labelColNuber = 0;
        private int locColNumber = 0;

        public Form1()
        {
            InitializeComponent();
            richTextBox1.Scroll += RTF_Scroll;
            richTextBox2.Scroll += RTF_Scroll;
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() != DialogResult.OK)
                return;
            var tmp = openFileDialog1.FileName;
            
            if(LoadResFile(tmp))
                label3.Text = tmp;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() != DialogResult.OK)
                return;
            var tmp = openFileDialog1.FileName;
            label2.Text = tmp;
            LoadBaseFile(tmp);
        }

        private string[] ReadCSV(string path)
        {
            try
            {
                return File.ReadAllLines(path);
            }
            catch (Exception e) { MessageBox.Show(e.Message, "Ошибка загрузки"); return new string[0]; }
        }

        private void LoadBaseFile(string path)
        {
            master.Clear();
            var lines = ReadCSV(path);
            if (lines.Length == 0)
                return;
            for (int i = 1; i < lines.Length; i++)
            {
                string[] res = lines[i].Split(',');
                string key = String.Format("{0}:{1}", res[0], res[1]);

                if (!master.ContainsKey(key))
                    master.Add(key, lines[i]);
            }
            FillRBT();
        }

        private bool LoadResFile(string path)
        {
            slave.Clear();

            labelColNuber = -1;
            locColNumber = -1;

            var lines = ReadCSV(path);
            if (lines.Length == 0)
                return false;

            var res = lines[0].Split(',');

            for (int i = 0; i < res.Length; i++)
            {
                if (res[i].ToLower().Equals(labelCol.ToLower()))
                {
                    labelColNuber = i;
                    continue;
                }

                if (res[i].ToLower().Equals(locCol.ToLower()))
                    locColNumber = i;
            }

            if ((labelColNuber == -1) || (locColNumber == -1))
            {
                MessageBox.Show("Некорректный формат файла");
                return false;
            }

            for (int i = 1; i < lines.Length; i++)
            {
                var r = lines[i].Split(',');

                if ((r.Length < labelColNuber) || (r.Length < locColNumber))
                    continue;

                string key = String.Format("{0}:{1}", r[locColNumber], r[labelColNuber]);
                if (!slave.ContainsKey(key))
                    slave.Add(key, lines[i]);
            }

            FillRBT();

            return true;
        }

        private void FillRBT()
        {
            this.Enabled = false;
            richTextBox1.Text = "";
            richTextBox2.Text = "";
            List<string> mark = new List<string>();

            foreach (string s in master.Keys)
            {

                richTextBox1.Text += String.Format("{0}{1}", master[s], Environment.NewLine);

                if (!slave.Keys.Contains(s))
                {
                    richTextBox2.Text += String.Format("---{0}", Environment.NewLine);
                    mark.Add(master[s]);
                }
                else
                    richTextBox2.Text += String.Format("{0}{1}", slave[s], Environment.NewLine);
            }

            richTextBox1.SelectAll();
            richTextBox1.SelectionColor = Color.Black;

            foreach (string s in mark)
                Mark(s);

            this.Enabled = true;
        }

        private void Mark(string txt)
        {
            richTextBox1.SelectionStart = richTextBox1.Text.LastIndexOf(txt);
            richTextBox1.SelectionLength = txt.Length;
            richTextBox1.SelectionColor = Color.Red;
        }

        private bool isScrolling = false;// признак прокрутки контрола
        /// <summary>
        /// прокрутка
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RTF_Scroll(object sender, MessageEventArgs e)
        {
            if (!isScrolling)
            {
                isScrolling = true;

                ImprovedRichTextBox senderRtf = sender as ImprovedRichTextBox;
                ImprovedRichTextBox rtf = senderRtf == richTextBox1 ? richTextBox2 : richTextBox1;

                Message m = e.Message;
                m.HWnd = rtf.Handle;
                rtf.SendScrollMessage(m);

                isScrolling = false;
            }
        }

    }

    #region MessageEventHandler
    public class MessageEventArgs : EventArgs
    {
        /// <summary>
        /// сообщение
        /// </summary>
        public Message Message { get; private set; }

        /// <summary>
        /// конструктор
        /// </summary>
        public MessageEventArgs()
        {
        }

        /// <summary>
        /// конструктор
        /// </summary>
        /// <param name="msg"> сообщение </param>
        public MessageEventArgs(Message msg)
        {
            this.Message = msg;
        }
    }
    public delegate void MessageEventHandler(object sender, MessageEventArgs e);
    #endregion

    public class ImprovedRichTextBox : RichTextBox
    {
        #region WinAPI

        private const int WM_HSCROLL = 276;
        private const int WM_VSCROLL = 277;

        private const int SB_HORZ = 0;
        private const int SB_VERT = 1;

        [DllImport("user32.dll")]
        public static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);

        #endregion

        #region Constructors

        /// <summary>
        /// конструктор
        /// </summary>
        public ImprovedRichTextBox()
        {
        }

        #endregion

        #region Events

        public event MessageEventHandler Scroll;

        #endregion

        #region Protected methods

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HSCROLL || m.Msg == WM_VSCROLL)
            {
                OnScroll(m);
            }

            base.WndProc(ref m);
        }

        /// <summary>
        /// вызов события 'Scroll'
        /// </summary>
        /// <param name="m"></param>
        protected virtual void OnScroll(Message m)
        {
            if (Scroll != null) Scroll(this, new MessageEventArgs(m));
        }

        #endregion
        #region Public methods

        /// <summary>
        /// послать событие прокрутки
        /// </summary>
        /// <param name="m"></param>
        public void SendScrollMessage(Message m)
        {
            base.WndProc(ref m);

            // прокрутка
            switch (m.Msg)
            {
                case WM_HSCROLL:
                    SetScrollPos(Handle, SB_HORZ, m.WParam.ToInt32() >> 16, true);
                    break;
                case WM_VSCROLL:
                    SetScrollPos(Handle, SB_VERT, m.WParam.ToInt32() >> 16, true);
                    break;
            }
        }

        #endregion
    }
}
