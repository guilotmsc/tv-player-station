using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.DirectX.AudioVideoPlayback;
using System.IO;
using System.Management;
using System.Diagnostics;
using System.Data.SQLite;
using System.Net;

namespace MyVideoPlayer
{
    public partial class Main : Form
    {
        private static string connection = "Data Source=player.db";
        private static string namedb = "player.db";

        //for your info, this only works on x86 projects
        //due to the library itself

        private Video video;
        private string[] videoPaths;
        private string folderPath = @"C:\videos\";
        private int selectedIndex = 0;
        private Size formSize;
        private Size pnlSize;

        public Main()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            insere();
            // Cria a base de dados se ela nao existir
            createDB();
            
            // Inicializa o servidor em standby para receber atualizações
            HTTPServer server = new HTTPServer(4080);
            server.Start();


            // Seta tamanho da janela
            formSize = new Size(this.Width, this.Height);
            pnlSize = new Size(pnlVideo.Width, pnlVideo.Height);

            setPaths();

            // Inicializa campos e valores
            setFields();

        }

        private void setFields()
        {
            this.ipv4.Text = Dns.GetHostByName(Dns.GetHostName()).AddressList[0].ToString();
            this.tokenBox.Text = EncodeTo64(this.ipv4.Text);

            this.label3.Text = Environment.OSVersion.ToString();
            this.processador.Text = System.Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");

            if (Environment.Is64BitOperatingSystem == true)
                this.arquitetura.Text = "64-bit";
            else
                this.arquitetura.Text = "32-bit";
        }

        static public string EncodeTo64(string toEncode)
        {
            byte[] toEncodeAsBytes
                  = System.Text.ASCIIEncoding.ASCII.GetBytes(toEncode);
            string returnValue
                  = System.Convert.ToBase64String(toEncodeAsBytes);
            return returnValue;
        }

        private void setPaths()
        { 
            string sql = "SELECT NAME FROM VIDEOS";

            SQLiteConnection conn = new SQLiteConnection(connection);

            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SQLiteCommand cmd = new SQLiteCommand(sql, conn);
            SQLiteDataReader rd = cmd.ExecuteReader();

            DataTable datatable = new DataTable();
            datatable.Load(rd);

            foreach (DataRow dt in datatable.Rows)
            {
                dt["NAME"] = folderPath + dt["NAME"].ToString();
            }

            videoPaths = datatable.Rows.OfType<DataRow>().Select(k => k[0].ToString()).ToArray();
             
            if (videoPaths != null)
            {
                foreach (string path in videoPaths)
                {
                    string vid = path.Replace(folderPath, string.Empty);
                    vid = vid.Replace(".wmv", string.Empty);
                    lstVideos.Items.Add(vid);
                }
            }

            lstVideos.SelectedIndex = selectedIndex;
        }

        private void createDB()
        {
            if (!File.Exists(namedb))
            {
                SQLiteConnection.CreateFile(namedb);
                SQLiteConnection conn = new SQLiteConnection(connection);

                conn.Open();

                StringBuilder str = new StringBuilder();
                str.Append("CREATE TABLE IF NOT EXISTS VIDEOS (ID INTEGER PRIMARY KEY AUTOINCREMENT, NAME VARCHAR(30), STATUS BOOLEAN)");

                SQLiteCommand cmd = new SQLiteCommand(str.ToString(), conn);

                MessageBox.Show("Banco de dados criado com sucesso!");
                
                try
                {
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
                catch (Exception e)
                {
                    MessageBox.Show("Erro ao criar banco de dados: " + e.Message);
                }
                
            }
        }

        private void lstVideos_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                video.Stop();
                video.Dispose();
            }
            catch { }

            int index = lstVideos.SelectedIndex;
            selectedIndex = index;
            video = new Video(videoPaths[index], false);
            video.Owner = pnlVideo;
            pnlVideo.Size = pnlSize;
            video.Play();
            tmrVideo.Enabled = true; 
            video.Ending += Video_Ending; 
        }

        private void Video_Ending(object sender, EventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                System.Threading.Thread.Sleep(2000);

                if (InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        NextVideo();
                    }));
                }
            });
        }

        private void NextVideo()
        {
            int index = lstVideos.SelectedIndex;
            index++;
            if (index > videoPaths.Length - 1)
                index = 0;
            selectedIndex = index;
            lstVideos.SelectedIndex = index;
        }

        private void PreviousVideo()
        {
            int index = lstVideos.SelectedIndex;
            index--;
            if (index == -1)
                index = videoPaths.Length - 1;
            selectedIndex = index;
            lstVideos.SelectedIndex = index;
        }

        private void btnPlayPause_Click(object sender, EventArgs e)
        {
            if (!video.Playing)
            {
                video.Play();
                tmrVideo.Enabled = true; 
            }
            else if (video.Playing)
            {
                video.Pause();
                tmrVideo.Enabled = false; 
            }
        }

        private void btnFullscreen_Click(object sender, EventArgs e)
        {
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            video.Owner = this;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                //exit full screen when escape is pressed
                FormBorderStyle = FormBorderStyle.Sizable;
                WindowState = FormWindowState.Normal;
                this.Size = formSize;
                video.Owner = pnlVideo;
                pnlVideo.Size = pnlSize;
            }
        }
 
        private void timer1_Tick(object sender, EventArgs e)
        {
            lstVideos.Items.Clear();
            string sql = "SELECT NAME FROM VIDEOS";

            SQLiteConnection conn = new SQLiteConnection(connection);

            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SQLiteCommand cmd = new SQLiteCommand(sql, conn);
            SQLiteDataReader rd = cmd.ExecuteReader();

            DataTable datatable = new DataTable();
            datatable.Load(rd);


            foreach (DataRow dt in datatable.Rows)
            {
                int i = 0;
                videoPaths[i] = folderPath + dt["NAME"].ToString();
            }

            //videoPaths = Directory.GetFiles(folderPath, "*.wmv");

            if (videoPaths != null)
            {
                foreach (string path in videoPaths)
                {
                    string vid = path.Replace(folderPath, string.Empty);
                    vid = vid.Replace(".wmv", string.Empty);
                    lstVideos.Items.Add(vid);
                }
            }

            lstVideos.SelectedIndex = selectedIndex;
        }

        private void play_Click(object sender, EventArgs e)
        {
            if (!video.Playing)
            {
                video.Play();
                tmrVideo.Enabled = true;
            }
            else if (video.Playing)
            {
                video.Pause();
                tmrVideo.Enabled = false;
            }
        }

        public void updateData(DataTable datatable)
        {
            if(datatable.Rows.Count > 0)
            {
                String str = String.Empty;

                str = "INSERT INTO VIDEOS (NAME) VALUES ";

                foreach (DataRow row in datatable.Rows)
                {
                    str += "('" + row["video_file_name"].ToString() + "'),";
                }

                String sql = str.Remove(str.Length - 1);

                SQLiteConnection conn = new SQLiteConnection(connection);

                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }
                 
                SQLiteCommand cmd = new SQLiteCommand(sql, conn);

                try
                {
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
                catch (Exception e)
                {
                    MessageBox.Show("Erro ao inserir no banco de dados: " + e.Message);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            String sql = "SELECT * FROM VIDEOS";

            SQLiteConnection conn = new SQLiteConnection(connection);

            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SQLiteCommand cmd = new SQLiteCommand(sql, conn);
            SQLiteDataReader rd = cmd.ExecuteReader();

            DataTable dt = new DataTable();
            dt.Load(rd);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            String sql = "DELETE FROM VIDEOS";

            SQLiteConnection conn = new SQLiteConnection(connection);

            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SQLiteCommand cmd = new SQLiteCommand(sql, conn);

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            String sql = "UPDATE VIDEOS SET NAME = 'Video1.wmv'";
            
            SQLiteConnection conn = new SQLiteConnection(connection);

            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SQLiteCommand cmd = new SQLiteCommand(sql, conn);

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }

        private void insere()
        {
            //String sql = "UPDATE VIDEOS SET NAME = 'Video1.wmv'";
            String sql = "INSERT INTO VIDEOS (NAME) VALUES ('panasonic.wmv')";

            SQLiteConnection conn = new SQLiteConnection(connection);

            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SQLiteCommand cmd = new SQLiteCommand(sql, conn);

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }

        private void tokenBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void label10_Click(object sender, EventArgs e)
        {

        }
    }
}
