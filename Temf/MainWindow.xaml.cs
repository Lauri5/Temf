using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;
using Microsoft.WindowsAPICodePack.Shell;
using System.ComponentModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Collections.Generic; 
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Threading;
using System.Reflection;
using Microsoft.Win32;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Drawing;

namespace Temf
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SqlConnection sqlConnection;
        private System.Windows.Forms.NotifyIcon m_notifyIcon;

        public MainWindow()
        {
            InitializeComponent();

            StartUp();
            Properties.Settings.Default.Prova = true;
            this.components = new System.ComponentModel.Container();
            this.contextMenu1 = new System.Windows.Forms.ContextMenu();
            this.menuItem1 = new System.Windows.Forms.MenuItem();

            this.contextMenu1.MenuItems.AddRange(
                    new System.Windows.Forms.MenuItem[] { this.menuItem1 });

            this.menuItem1.Index = 0;
            this.menuItem1.Text = "E&xit";
            this.menuItem1.Click += new System.EventHandler(this.menuItem1_Click);


            this.ClientSize = new System.Drawing.Size(292, 266);
            this.Text = "Notify Icon Example";

            this.m_notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);

            m_notifyIcon.ContextMenu = this.contextMenu1;

            m_notifyIcon.Text = "Temf";
            m_notifyIcon.Icon = new System.Drawing.Icon("Logo.ico");
            m_notifyIcon.Click += new EventHandler(m_notifyIcon_Click);

            m_notifyIcon.ContextMenu = this.contextMenu1;

            CheckForIsRunningApplication();
            string connectionString = ConfigurationManager.ConnectionStrings["Temf.Properties.Settings.TemfDBConnectionString"].ConnectionString;
            sqlConnection = new SqlConnection(connectionString);
            installed_Apps();
            ShowApps();

        }

        private void menuItem1_Click(object sender, EventArgs e)
        {
            StartUp();
            m_notifyIcon.Dispose();
            m_notifyIcon = null;

            Properties.Settings.Default.Top = this.Top;
            Properties.Settings.Default.Left = this.Left;
            Properties.Settings.Default.Height = this.Height;
            Properties.Settings.Default.Width = this.Width;

            Properties.Settings.Default.Save();
            System.Windows.Application.Current.Shutdown();
        }

        #region Methods

        private void CheckForIsRunningApplication()
        {

            string strProcessName = Process.GetCurrentProcess().ProcessName;

            Process[] strAllProcesses = Process.GetProcessesByName(strProcessName);

            if (strAllProcesses.Length > 1)
            {
                MessageBox.Show("Application is already running.");
                Application.Current.Shutdown();
            }
        }
        private void appDaControllare()
        {
            foreach (System.Data.DataRowView dr in appsData.ItemsSource)
            {
                if (ProgramIsRunning(dr[1].ToString(), dr[2].ToString()) == true)
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        int tempo = GetTime(int.Parse(dr[0].ToString()));
                        UpdateTime(int.Parse(dr[0].ToString()), tempo);
                    }));
                }
            }
        }

        private int ContaParole(string nome)
        {
            try
            {
                int wordCount = 0, index = 0;


                while (index < nome.Length && char.IsWhiteSpace(nome[index]))
                    index++;

                while (index < nome.Length)
                {

                    while (index < nome.Length && !char.IsWhiteSpace(nome[index]))
                        index++;

                    wordCount++;

                    while (index < nome.Length && char.IsWhiteSpace(nome[index]))
                        index++;
                }

                return wordCount;
            }
            catch
            {
                return -1;
            }
        }

        private bool ProgramIsRunning(string path, string name)
        {
            Process[] processes = Process.GetProcessesByName($"{name}");
            if (processes.Length > 0)
            {
                return true;
            }
            else
            {
                Process[] processes1 = Process.GetProcessesByName($"{path}");
                if (processes1.Length > 0)
                {
                    return true;
                }
                else if (ContaParole(name) > 1)
                {
                    string firstWord = name.Substring(0, name.IndexOf(" "));
                    Process[] processes2 = Process.GetProcessesByName($"{firstWord}");
                    if (processes2.Length > 0)
                    {
                        return true;
                    }
                    else
                    {
                        string lastWord = name.Substring(name.LastIndexOf(" ") + 1);
                        Process[] processes3 = Process.GetProcessesByName($"{firstWord}");
                        if (processes3.Length > 0)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            return false;
            
        }


        private void StartUp()
        {

            if (Properties.Settings.Default.StartUp)
            {
                RegistryKey reg = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (reg.GetValue(Assembly.GetEntryAssembly().GetName().Name) == null)
                    reg.SetValue(Assembly.GetEntryAssembly().GetName().Name, "\"C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs\\Temf.lnk\" Ma");

            }
            else
            {
               
                RegistryKey reg = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (reg.GetValue(Assembly.GetEntryAssembly().GetName().Name) != null)
                reg.DeleteValue(Assembly.GetEntryAssembly().GetName().Name);
            }

        }

        private void installed_Apps()
        {
            var FODLERID_AppsFolder = new Guid("{1e87508d-89c2-42f0-8a7e-645a0f50ca58}");
            ShellObject appsFolder = (ShellObject)KnownFolderHelper.FromKnownFolderId(FODLERID_AppsFolder);

            foreach (var app in (IKnownFolder)appsFolder)
            {
                string name = app.Name;

                string path = System.IO.Path.GetFileNameWithoutExtension(app.Properties.System.Link.TargetParsingPath.Value);
                Installed installed = new Installed();

                installed.installed_Path = path;
                installed.installed_Name = name;
                installedData.Items.Add(installed);
            }

            SortDataGrid(installedData, 1);
        }

        private int GetTime(int _id)
        {
            try
            {
                string query = $"select time from Applicazioni where Id = {_id}";
                SqlCommand com = new SqlCommand(query, sqlConnection);

                sqlConnection.Open();
                int tempo = 0;

                using (SqlDataReader read = com.ExecuteReader())
                {
                    while (read.Read())
                    {
                        tempo = int.Parse(read["time"].ToString());
                    }
                    return tempo;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return 0;
            }
            finally
            {
                sqlConnection.Close();
            }
        }

        private void UpdateTime(int _id, int tempo)
        {
            try
            {
                string query = $"update Applicazioni set time={tempo + 30} where Id=@Id";
                SqlCommand sqlCommand = new SqlCommand(query, sqlConnection);

                sqlCommand.Parameters.AddWithValue("@Id", _id);
                sqlConnection.Open();
                sqlCommand.ExecuteScalar();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

            }
            finally
            {
                sqlConnection.Close();
                ShowApps();
            }
        }

        private void ShowApps()
        {
            
            try
            {
                string query = "select Id, path, name, CAST(time / 3600 AS DECIMAL(16,2)) as time from Applicazioni";
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(query, sqlConnection);
                sqlConnection.Open();
                string CmdString = string.Empty;
            
            
                using (sqlDataAdapter)
                {
                    DataTable applicazioni = new DataTable();
            
                    sqlDataAdapter.Fill(applicazioni);
                    
                    appsData.SelectedValuePath = "Id";
                    appsData.ItemsSource = applicazioni.DefaultView;
                }
            
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            finally
            {
                sqlConnection.Close();
            }
        }
        private void AddApps(string path, string name, decimal time)
         {
            try
            {
                string query = $"insert into Applicazioni (path, name, time) values ('{path}', '{name}', {Math.Round(time * 3600)})";
                SqlCommand sqlCommand = new SqlCommand(query, sqlConnection);
                sqlConnection.Open();
                sqlCommand.ExecuteScalar();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

            }
            finally
            {
                sqlConnection.Close();
                ShowApps();
            }
        }

        private static void SortDataGrid(DataGrid dataGrid, int columnIndex, ListSortDirection sortDirection = ListSortDirection.Ascending)
        {
            var column = dataGrid.Columns[columnIndex];

            dataGrid.Items.SortDescriptions.Clear();

            dataGrid.Items.SortDescriptions.Add(new SortDescription(column.SortMemberPath, sortDirection));

            foreach (var col in dataGrid.Columns)
            {
                col.SortDirection = null;
            }
            column.SortDirection = sortDirection;

            dataGrid.Items.Refresh();
        }
        void CheckTrayIcon()
        {
            //ShowTrayIcon(!IsVisible);
            if (m_notifyIcon != null)
                m_notifyIcon.Visible = true;
        }

        void ShowTrayIcon(bool show)
        {
            if (m_notifyIcon != null)
                m_notifyIcon.Visible = show;
        }

        #endregion

        #region Buttons


        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            installedData.Items.Clear();
            installed_Apps();
        }

        private void Hyperlink_RequestNavigate(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://paypal.me/GabrieleLauricella");
        }
        private void Hyperlink_RequestNavigate1(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://youtu.be/4qBouGMASxI");
        }
        private void installedData_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        private void DataGridRow_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Installed item = (Installed)installedData.SelectedItem;

            if (!appsData.Items.Contains(item.installed_Name))
            {
                AddApps(item.installed_Path, item.installed_Name, 0);
            }
        }


        private void appsData_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                try
                {
                    string query = "delete from Applicazioni where id = @Id";
                    SqlCommand sqlCommand = new SqlCommand(query, sqlConnection);
                    sqlConnection.Open();
                    sqlCommand.Parameters.AddWithValue("@Id", appsData.SelectedValue);
                    sqlCommand.ExecuteScalar();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);

                }
                finally
                {
                    sqlConnection.Close();
                    ShowApps();
                    if (appsData.Items.Count <= 38)
                        appsData.Height = double.NaN;
                }
            }
            else if (e.Key != Key.Delete)
            { }

        }

        private void installedData_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                Installed item = (Installed)installedData.SelectedItem;

                if (!appsData.Items.Contains(item.installed_Name))
                {
                    AddApps(item.installed_Path, item.installed_Name, 0);
                }

            }
        }

        private void installedData_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {

        }


        private void border_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string query = "delete from Applicazioni where id = @Id";
                SqlCommand sqlCommand = new SqlCommand(query, sqlConnection);
                sqlConnection.Open();
                sqlCommand.Parameters.AddWithValue("@Id", appsData.SelectedValue);
                sqlCommand.ExecuteScalar();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                sqlConnection.Close();
                ShowApps();
                if (appsData.Items.Count <= 38)
                    appsData.Height = double.NaN;
            }
        }


        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            DataRowView path = (DataRowView)appsData.SelectedItem;
            DataRowView name = (DataRowView)appsData.SelectedItem;

            if (ProgramIsRunning(path[1].ToString(), name[2].ToString()))
            {
                MessageBox.Show("The selected app is running!");
            }
            else
            {
                MessageBox.Show("The selected app is not running!");
            }
        }

        private void Delete__Click(object sender, RoutedEventArgs e)
        {
            Path.Clear();
            Name.Clear();
            Time.Clear();
        }


        private void Add__Click(object sender, RoutedEventArgs e)
        {
            
            try
            {
                if (appsData.SelectedItems.Count == 0 && Path.Text != "" && Name.Text != "" && Time.Text != "")
                    AddApps(Path.Text, Name.Text, Convert.ToDecimal(Time.Text));
                else 
                {
                    decimal asd = Convert.ToDecimal(Time.Text) ;
                    string query = $"update Applicazioni set path='{Path.Text}', name='{Name.Text}', time={Math.Round(asd * 3600)} where Id=@Id";
                    SqlCommand sqlCommand = new SqlCommand(query, sqlConnection);
                    if (appsData.SelectedValue != null) 
                    {
                        sqlCommand.Parameters.AddWithValue("@Id", appsData.SelectedValue);
                        sqlConnection.Open();
                        sqlCommand.ExecuteScalar();
                    }
                    else { MessageBox.Show("Select a row"); }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

            }
            finally
            {
                sqlConnection.Close();
                ShowApps();
            }
        }
        private void CheckStartup_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.StartUp = true;
            Properties.Settings.Default.Save();

        }

        private void CheckStartup_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.StartUp = false;
            Properties.Settings.Default.Save();

        }

        void m_notifyIcon_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = m_storedWindowState;
        }
        #endregion

        #region Properties
        private class Installed
        {
            public string installed_Name { get; set; }
            public string installed_Path { get; set; }
        }

        #endregion


        private void mainWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (appsData.Items.Count <= 38)
                appsData.Height = double.NaN;
            Task task = new Task(() =>
            {

                while (true)
                {
                    Thread.Sleep(30000);
                    appDaControllare();
                }

            });
            task.Start();


        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;

            Properties.Settings.Default.Prova = false;
            WindowState = WindowState.Minimized;
        }

        private WindowState m_storedWindowState = WindowState.Normal;
        private Container components;
        private System.Windows.Forms.ContextMenu contextMenu1;
        private System.Windows.Forms.MenuItem menuItem1;

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized && Properties.Settings.Default.Prova == false)
            {
                Hide();
                Properties.Settings.Default.Prova = true;
            }
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            CheckTrayIcon();
        }
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_STYLE = -16;
        private const int WS_MAXIMIZEBOX = 0x10000;

        public System.Drawing.Size ClientSize { get; }
        public string Text { get; }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            Properties.Settings.Default.Prova = false;
            WindowState = WindowState.Minimized; 

            CheckStartup.IsChecked = Properties.Settings.Default.StartUp;

            var hwnd = new WindowInteropHelper((Window)sender).Handle;
            var value = GetWindowLong(hwnd, GWL_STYLE);
            SetWindowLong(hwnd, GWL_STYLE, (int)(value & ~WS_MAXIMIZEBOX));

            this.Top = Properties.Settings.Default.Top;
            this.Left = Properties.Settings.Default.Left;
            this.Height = Properties.Settings.Default.Height;
            this.Width = Properties.Settings.Default.Width;

        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Primo_Scroll.Height = this.Height - 90;
            if (appsData.ActualHeight > (this.Height - 90))
            appsData.Height = this.Height - 90;
            else
                appsData.Height = double.NaN;
        }
    }
     
}