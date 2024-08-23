using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Erasmus
{
    /// <summary>
    /// Interaction logic for NewEmailWindow.xaml
    /// </summary>
    public partial class NewEmailWindow : Window
    {
        public List<string> AttachedFiles { get; set; } = new List<string>();
 
        public Email ResultEmail { get; private set; }
        public bool IsDraft { get; private set; }
        public ObservableCollection<string> Categories { get; set; }

        public void SaveCategories()
        {
            Properties.Settings.Default.Categories = new System.Collections.Specialized.StringCollection();
            foreach (var category in Categories)
            {
                Properties.Settings.Default.Categories.Add(category);
            }
            Properties.Settings.Default.Save();
        }
 
        public NewEmailWindow()
        {
            InitializeComponent();
            LoadCategories(); 
            if (Properties.Settings.Default.Categories != null)
            {
                Categories = new ObservableCollection<string>(Properties.Settings.Default.Categories.Cast<string>());
            }
            else
            {
                Categories = new ObservableCollection<string>();
            }

            // Assuming you're using this directly in the code-behind:
            this.DataContext = this; // Set the DataContext for data binding
        }
        private void LoadCategories()
        {
            // Assuming Categories is stored in application settings as a StringCollection
            if (Properties.Settings.Default.Categories != null)
            {
                Categories = new ObservableCollection<string>(Properties.Settings.Default.Categories.Cast<string>());
            }
            else
            {
                Categories = new ObservableCollection<string>();
                // Optionally, add default categories if the list is supposed to have default values
                Categories.Add("Category 1");
               
                // Save the default categories back to settings if needed
                Properties.Settings.Default.Categories = new System.Collections.Specialized.StringCollection();
                Properties.Settings.Default.Categories.AddRange(Categories.ToArray());
                Properties.Settings.Default.Save();
            } 

        } 
        private void btnSend_Click(object sender, RoutedEventArgs e)
        {

            ResultEmail = CreateEmail();
            IsDraft = false; 

            this.DialogResult = true;
            
        }

        private void btnSaveAsDraft_Click(object sender, RoutedEventArgs e)
        {
            ResultEmail = CreateEmail();
            IsDraft = true;
            ResultEmail.IsDraft = true; 
            this.DialogResult = true;
        }
        private void btnAddAttachment_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true; // Allow multiple file selection
            openFileDialog.Filter = "All files (*.*)|*.*"; // Filter to include all files

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    AttachedFiles.Add(filename);
                    
                }

                // Update the UI or handle the list of files as needed
                UpdateAttachmentsUI();
            }
        }
        private void UpdateAttachmentsUI()
        {
            // Assuming there is a ListBox named lstAttachments to display the file names
            lstAttachments.Items.Clear();
            foreach (string file in AttachedFiles)
            {
                lstAttachments.Items.Add(file);
            }
        }
 

        private Email CreateEmail()
        {
            // Create and return a new Email object based on input
            var recipients = new ObservableCollection<string>(txtRecipients.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)); 
            var subject = txtSubject.Text;
            var content = new TextRange(rtbContent.Document.ContentStart, rtbContent.Document.ContentEnd).Text;
            var category = cmbCategories.SelectedItem?.ToString() ?? "DefaultCategory";

            if ( category == null)
            {
                return new Email("me@example.com", recipients, subject, content, AttachedFiles, "none");
            }

            else
            {
                return new Email("me@example.com", recipients, subject, content, AttachedFiles, category); 
            }

          
        } 
    }
}
