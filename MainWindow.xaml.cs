using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using OtpNet;
using System.IO;
using System.Text.Json.Serialization;
using QRCoder;
using Microsoft.Win32;
using ZXing;
using System.Security.Principal;
using System.Web;
using static QRCoder.QRCodeGenerator;

namespace AuthWin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<Account> accounts = new ObservableCollection<Account>();
        DispatcherTimer timer = new DispatcherTimer();
        DispatcherTimer timer2 = new DispatcherTimer();
        string EncPassword = "0297D45A92EC5382428A1E387FEDC12DE0BBD0DA54E20D9E37D64CD170A5BFC1";
        System.Drawing.Bitmap qrCodeImage;
        bool EditMode = false;
        int EditIndex = -1;
        string AccounsFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\AuthWin\\accounts.json";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!System.IO.Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\AuthWin")) {
                System.IO.Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\AuthWin");
            }

            ReadJson();
            for (int i = 0; i < accounts.Count; i++)
            {
                Account acc = accounts[i];
                GenerateCode(ref acc);
                accounts[i] = acc;
            }
            lbCodes.ItemsSource = accounts;
            timer.Tick += timer_Tick;
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Start();

            timer2.Tick += timer2_Tick;
            timer2.Interval = new TimeSpan(0, 0, 3);
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            lbCodes.Visibility = Visibility.Collapsed;
            pnlBottom.Visibility = Visibility.Collapsed;
            grdAdd.Visibility = Visibility.Visible;
            grdAddButtons.Visibility = Visibility.Visible;

            txtSecret.IsEnabled = true;
            txtDuration.IsEnabled = true;
            txtLength.IsEnabled = true;
            cmbAlgo.IsEnabled = true;
            btnAddAccountManual.Visibility = Visibility.Visible;
            btnEditAccount.Visibility = Visibility.Collapsed;

            EditMode = false;
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            lbCodes.Visibility = Visibility.Visible;
            pnlBottom.Visibility = Visibility.Visible;
            grdAdd.Visibility = Visibility.Collapsed;
            grdManual.Visibility = Visibility.Collapsed;
        }

        private void btnAddManual_Click(object sender, RoutedEventArgs e)
        {
            if (grdManual.Visibility == Visibility.Collapsed) {
                grdManual.Visibility = Visibility.Visible;

                txtName.Text = string.Empty;
                txtIssuer.Text = string.Empty;
                txtSecret.Text = string.Empty;
                txtDuration.Text = "30";
                txtLength.Text = "6";
                cmbAlgo.SelectedIndex = 0;

                txtName.Focus();
            }
        }

        private void lblAdvance_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (grdAdvance.Visibility == Visibility.Collapsed)
            {
                grdAdvance.Visibility = Visibility.Visible;
                lblAdvance.Content = "\u2BC6 Advance";
            }
            else
            {
                grdAdvance.Visibility = Visibility.Collapsed;
                lblAdvance.Content = "\u2BC8 Advance";
            }

        }

        private void btnAddAccountManual_Click(object sender, RoutedEventArgs e)
        {
            bool OK = true;
            Account account = new Account();

            if (string.IsNullOrEmpty(txtName.Text))
            {
                txtName.Background = Brushes.LightCoral;
                OK = false;
            }
            else
            {
                account.Name = txtName.Text.Trim();
                txtName.Background = null;
            }

            /*
            if (string.IsNullOrEmpty(txtIssuer.Text))
            {
                txtIssuer.Background = Brushes.LightCoral;
                OK = false;
            }
            else
            {
                account.Issuer = txtIssuer.Text.Trim();
                txtIssuer.Background = null;
            }*/
            account.Issuer = txtIssuer.Text.Trim();

            if (string.IsNullOrEmpty(txtSecret.Text))
            {
                txtSecret.Background = Brushes.LightCoral;
                OK = false;
            }
            else
            {
                account.Secret = txtSecret.Text.Trim();
                txtSecret.Background = null;
            }

            if (grdAdvance.Visibility == Visibility.Visible)
            {
                if (!IsNum(txtDuration.Text))
                {
                    txtDuration.Text = "30";
                }
                if (!IsNum(txtLength.Text))
                {
                    txtLength.Text = "6";
                }
                account.Duration = Int32.Parse(txtDuration.Text);
                account.Length = Int32.Parse(txtLength.Text);
                account.HashAlgo = (Account.Hash)Enum.ToObject(typeof(Account.Hash), cmbAlgo.SelectedIndex);
            }
            else
            {
                account.Duration = 30;
                account.Length = 6;
                account.HashAlgo = Account.Hash.SHA1;
            }

            if (OK)
            {
                GenerateCode(ref account);
                account.Id = accounts.Count;
                accounts.Add(account);
                WriteJson();

                txtName.Text = "";
                txtIssuer.Text = "";
                txtSecret.Text = "";
                txtDuration.Text = "30";
                cmbAlgo.SelectedIndex = 0;
                txtLength.Text = "6";
                lbCodes.Visibility = Visibility.Visible;
                pnlBottom.Visibility = Visibility.Visible;
                grdAdd.Visibility = Visibility.Collapsed;
                grdManual.Visibility = Visibility.Collapsed;
            }
        }

        private bool IsNum(string txt)
        {
            var isNumeric = int.TryParse(txt, out int n);
            if (isNumeric)
            {
                if (n < 1)
                    return false;
                else
                    return true;
            }
            else
                return false;
        }

        private void GenerateCode(ref Account acc)
        {
            var base32Bytes = Base32Encoding.ToBytes(acc.Secret);
            var totp = new Totp(base32Bytes, acc.Duration, (OtpHashMode)Enum.ToObject(typeof(OtpHashMode), acc.HashAlgo), acc.Length);
            var totpCode = totp.ComputeTotp();
            acc.Totp = totpCode.ToString().Insert(totpCode.ToString().Length / 2, " ");
            acc.Seconds = totp.RemainingSeconds();
        }

        private string GetUri(Account acc)
        {
            string Uri = new OtpUri(OtpType.Totp, acc.Secret, acc.Name, acc.Issuer, (OtpHashMode)Enum.ToObject(typeof(OtpHashMode), acc.HashAlgo), acc.Length, acc.Duration).ToString();
            Uri = Uri.Replace("&algorithm=SHA1", "").Replace("&digits=6", "").Replace("&period=30", "");
            return Uri;
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < accounts.Count; i++)
            {
                if (accounts[i].Seconds > 1)
                    accounts[i].Seconds--;
                else
                {
                    Account acc = accounts[i];
                    GenerateCode(ref acc);
                    accounts[i] = acc;
                }
            }
            lbCodes.Items.Refresh();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            lblCopy.Content = "Click on a code to copy";
            lblCopy.Foreground = Brushes.Gray;
            lblCopy.Background = null;
            timer2.Stop();
        }

        private void WriteJson()
        {
            var opt = new JsonSerializerOptions() { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(accounts, opt);
            string encJson = EncDec.Encrypt(jsonString, EncPassword);
            using (StreamWriter sw = new StreamWriter(AccounsFile))
            {
                sw.Write(encJson);
                sw.Close();
            }
        }

        private void ReadJson()
        {
            try
            {
                if (System.IO.File.Exists(AccounsFile))
                {
                    using (StreamReader sr = new StreamReader(AccounsFile))
                    {
                        string encString = sr.ReadToEnd();
                        string jsonString = EncDec.Decrypt(encString, EncPassword).Trim('\0');
                        accounts = JsonSerializer.Deserialize<ObservableCollection<Account>>(jsonString);
                        for (int i = 0; i < accounts.Count; i++)
                        {
                            accounts[i].Id = i;
                        }
                    }
                }
            }
            catch
            {
                MessageBox.Show("Data seems corrupted!", "AuthWin", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void lbCodes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbCodes.SelectedIndex > -1)
            {
                Clipboard.SetText(accounts[lbCodes.SelectedIndex].Totp.Replace(" ", ""));
                lblCopy.Content = "Code copied to clipboard";
                lblCopy.Foreground = Brushes.Blue;
                lblCopy.Background = Brushes.Yellow;
                timer2.Stop();
                timer2.Start();
                lbCodes.SelectedIndex = -1;
            }
        }

        private void QR_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            int i = (int)((System.Windows.Controls.Image)sender).Tag;

            string Uri = GetUri(accounts[i]);
            txtUri.Text = Uri;

            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(Uri, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            qrCodeImage = qrCode.GetGraphic(10);

            using (var ms = new MemoryStream())
            {
                qrCodeImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;

                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.StreamSource = ms;
                bi.EndInit();

                imgQR.Source = bi;
            }

            lbCodes.Visibility = Visibility.Collapsed;
            grdQR.Visibility = Visibility.Visible;
            lblCopy.Visibility = Visibility.Hidden;
            pnlBottom.Visibility = Visibility.Hidden;

            e.Handled = true;
        }

        private void Edit_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            e.Handled = true;
            int i = (int)((Image)sender).Tag;
            EditIndex = i;

            lbCodes.Visibility = Visibility.Collapsed;
            pnlBottom.Visibility = Visibility.Collapsed;
            grdAdd.Visibility = Visibility.Visible;
            grdAddButtons.Visibility = Visibility.Hidden;

            EditMode = true;

            txtName.Text = accounts[i].Name;
            txtIssuer.Text = accounts[i].Issuer;
            txtSecret.Text = accounts[i].Secret;
            txtDuration.Text = accounts[i].Duration.ToString();
            txtLength.Text = accounts[i].Length.ToString();
            cmbAlgo.SelectedIndex = (int)accounts[i].HashAlgo;

            txtSecret.IsEnabled = false;
            txtDuration.IsEnabled = false;
            txtLength.IsEnabled = false;
            cmbAlgo.IsEnabled = false;
            btnAddAccountManual.Visibility = Visibility.Collapsed;
            btnEditAccount.Visibility = Visibility.Visible;

            grdManual.Visibility = Visibility.Visible;
        }

        private void Delete_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            int i = (int)((Image)sender).Tag;
            string Name = accounts[i].Issuer + " " + accounts[i].Name;
            if (MessageBox.Show(String.Format("Are you sure you want to delete {0}?", Name), "AuthWin", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                accounts.RemoveAt(i);
            }
            for (int n = 0; n < accounts.Count; n++)
            {
                accounts[n].Id = n;
            }
            lbCodes.Items.Refresh();
            WriteJson();
            e.Handled = true;
        }

        private void Image_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Image)sender).Opacity = 1;
        }

        private void Image_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Image)sender).Opacity = 0.4;
        }

        private void btnCloseQR_Click(object sender, RoutedEventArgs e)
        {
            grdQR.Visibility = Visibility.Collapsed;
            lbCodes.Visibility = Visibility.Visible;
            lblCopy.Visibility = Visibility.Visible;
            pnlBottom.Visibility = Visibility.Visible;
            btnCopyUri.Content = "Copy";
        }

        private void btnCopyUri_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(txtUri.Text);
            btnCopyUri.Content = "Copied";
        }

        private void btnSaveQR_Click(object sender, RoutedEventArgs e)
        {
            if (qrCodeImage != null)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.AddExtension = true;
                sfd.Filter = "PNG Image|*.png";
                sfd.FileName = "QRCode.png";
                if (sfd.ShowDialog() == true)
                {
                    qrCodeImage.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Png);
                }
                sfd = null;
            }
        }

        private void btnScanQR_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Images|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff;*.gif";
            if (ofd.ShowDialog() == true)
            {
                IBarcodeReader reader = new BarcodeReader();
                var barcodeBitmap = (System.Drawing.Bitmap)System.Drawing.Image.FromFile(ofd.FileName);
                var result = reader.Decode(barcodeBitmap);
                if (result != null)
                {
                    string Uri = result.Text;
                    if (Uri.StartsWith("otpauth://totp/"))
                    {
                        Account acc = new Account();

                        Uri = Uri.Replace("otpauth://totp/", "");
                        string ni = Uri.Substring(0, Uri.IndexOf("?"));
                        ni = System.Web.HttpUtility.UrlDecode(ni);
                        acc.Issuer = ni.Split(':')[0];
                        acc.Name = ni.Split(':')[1];

                        string qs = Uri.Substring(Uri.IndexOf("?") + 1);
                        string[] qsparams = qs.Split('&');
                        foreach (string qsparam in qsparams)
                        {
                            string[] parts = qsparam.Split('=');
                            switch (parts[0].ToLower())
                            {
                                case "secret":
                                    acc.Secret = parts[1];
                                    break;
                                case "digits":
                                    acc.Length = Convert.ToInt32(parts[1]);
                                    break;
                                case "period":
                                    acc.Duration = Convert.ToInt32(parts[1]);
                                    break;
                                case "algorithm":
                                    acc.HashAlgo = (Account.Hash)Enum.Parse(typeof(Account.Hash), parts[1], true);
                                    break;
                            }
                        }

                        acc.Id = accounts.Count;
                        GenerateCode(ref acc);
                        accounts.Add(acc);
                        WriteJson();

                        lbCodes.Visibility = Visibility.Visible;
                        pnlBottom.Visibility = Visibility.Visible;
                        grdAdd.Visibility = Visibility.Collapsed;
                        grdManual.Visibility = Visibility.Collapsed;
                    }
                    else if (Uri.StartsWith("otpauth-migration:")) {
                        try
                        {
                            Uri = Uri.Replace("otpauth-migration://offline?data=", "");
                            Uri = HttpUtility.UrlDecode(Uri);
                            byte[] bytes = System.Convert.FromBase64String(Uri);
                            MigrationPayload payload = new MigrationPayload();
                            payload = MigrationPayload.Parser.ParseFrom(bytes);

                            if (payload.OtpParameters.Count > 0) {
                                for (int i = 0; i < payload.OtpParameters.Count; i++)
                                {
                                    Account acc = new Account();
                                    acc.Name = payload.OtpParameters[i].Name;
                                    acc.Issuer = payload.OtpParameters[i].Issuer;
                                    acc.Secret = Base32.ToBase32String(payload.OtpParameters[i].Secret.ToByteArray());
                                    if (payload.OtpParameters[i].Digits == 2) acc.Length = 8;

                                    acc.Id = accounts.Count;
                                    GenerateCode(ref acc);
                                    accounts.Add(acc);
                                }
                                WriteJson();
                            }
                        }
                        catch {
                            MessageBox.Show("Error reading Google Authenticator code", "AuthWin", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }
                        finally {
                            lbCodes.Visibility = Visibility.Visible;
                            pnlBottom.Visibility = Visibility.Visible;
                            grdAdd.Visibility = Visibility.Collapsed;
                            grdManual.Visibility = Visibility.Collapsed;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Not a valid QR code", "AuthWin", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                }
                else
                {
                    MessageBox.Show("Cannot read the QR code!", "AuthWin", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            if (pnlExport.Visibility == Visibility.Collapsed)
            {
                pnlExport.Visibility = Visibility.Visible;
                pnlImport.Visibility = Visibility.Collapsed;
            }
            else {
                pnlExport.Visibility = Visibility.Collapsed;
            }
        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            if (pnlImport.Visibility == Visibility.Collapsed)
            {
                pnlImport.Visibility = Visibility.Visible;
                pnlExport.Visibility = Visibility.Collapsed;
            }
            else {
                pnlImport.Visibility = Visibility.Collapsed;
            }
        }

        private void btnEditAccount_Click(object sender, RoutedEventArgs e)
        {
            if (EditMode) {
                if (string.IsNullOrEmpty(txtName.Text))
                {
                    txtName.Background = Brushes.LightCoral;
                    return;
                }
                else
                {
                    accounts[EditIndex].Name = txtName.Text.Trim();
                    txtName.Background = null;
                }

                accounts[EditIndex].Issuer = txtIssuer.Text.Trim();
                lbCodes.Items.Refresh();
                WriteJson();

                lbCodes.Visibility = Visibility.Visible;
                pnlBottom.Visibility = Visibility.Visible;
                grdAdd.Visibility = Visibility.Collapsed;
                grdManual.Visibility = Visibility.Collapsed;
                grdAddButtons.Visibility = Visibility.Visible;
                btnEditAccount.Visibility = Visibility.Collapsed;
                btnAddAccountManual.Visibility = Visibility.Visible;
                EditMode = false;
            }
        }

        private void lblWeb_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("https://authwin.com");
        }

        private void chkExportPass_Checked(object sender, RoutedEventArgs e)
        {
            txtExportPass.Visibility = Visibility.Visible;
            txtExportPass.Focus();
        }

        private void chkExportPass_Unchecked(object sender, RoutedEventArgs e)
        {
            txtExportPass.Visibility = Visibility.Hidden;
        }

        private void btnExportFinal_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = "Accounts.json";
            sfd.AddExtension = true;
            sfd.Filter = "JSON File|*.json";
            if (sfd.ShowDialog() == true)
            {
                var opt = new JsonSerializerOptions() { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(accounts, opt);
                using (StreamWriter sw = new StreamWriter(sfd.FileName))
                {
                    if (chkExportPass.IsChecked == true && !String.IsNullOrEmpty(txtExportPass.Text)) {
                        string PassHash = EncDec.Sha256(txtExportPass.Text.Trim());
                        string Encoded = EncDec.Encrypt(jsonString, PassHash);
                        sw.Write(Encoded);
                    }
                    else {
                        sw.Write(jsonString);
                    }
                    sw.Close();
                }
                MessageBox.Show("Accounts exported, keep it safe!", "AuthWin", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            sfd = null;
            pnlExport.Visibility = Visibility.Collapsed;
        }

        private void btnImportFinal_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "JSON File|*.json";
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    using (StreamReader sr = new StreamReader(ofd.FileName))
                    {
                        string jsonString = sr.ReadToEnd();

                        if (chkImportPass.IsChecked == true && !String.IsNullOrEmpty(txtImportPass.Text))
                        {
                            string PassHash = EncDec.Sha256(txtImportPass.Text.Trim());
                            jsonString = EncDec.Decrypt(jsonString, PassHash);
                        }

                        ObservableCollection<Account> imported = JsonSerializer.Deserialize<ObservableCollection<Account>>(jsonString);
                        for (int i = 0; i < imported.Count; i++)
                        {
                            accounts.Add(imported[i]);
                        }
                        for (int i = 0; i < accounts.Count; i++)
                        {
                            Account acc = accounts[i];
                            acc.Id = i;
                            GenerateCode(ref acc);
                            accounts[i] = acc;
                        }
                        lbCodes.Items.Refresh();
                        WriteJson();
                    }
                }
                catch { }
            }
            ofd = null;
            pnlImport.Visibility = Visibility.Collapsed;
        }

        private void chkImportPass_Checked(object sender, RoutedEventArgs e)
        {
            txtImportPass.Visibility = Visibility.Visible;
            txtImportPass.Focus();
        }

        private void chkImportPass_Unchecked(object sender, RoutedEventArgs e)
        {
            txtImportPass.Visibility = Visibility.Hidden;
        }
    }

    public class Account
    {
        public string Name { get; set; }
        public string Issuer { get; set; }
        public string Secret { get; set; }
        public int Duration { get; set; }
        public int Length { get; set; }
        public Hash HashAlgo { get; set; }

        [JsonIgnore] public string Totp { get; set; }
        [JsonIgnore] public int Seconds { get; set; }
        [JsonIgnore] public int Id { get; set; }

        public enum Hash
        {
            SHA1,
            SHA256,
            SHA512
        }

        public Account()
        {
            Duration = 30;
            Length = 6;
            HashAlgo = Hash.SHA1;
        }
    }
}
