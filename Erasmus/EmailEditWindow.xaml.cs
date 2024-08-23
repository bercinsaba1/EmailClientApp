using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Erasmus
{
    public partial class EmailEditWindow : Window
    {
        private EmailClientViewModel _viewModel;
        private Email _temporaryEmail;

        public EmailEditWindow(EmailClientViewModel viewModel)
        {
            InitializeComponent();
   
            _viewModel = viewModel;

            // Set the DataContext to the passed-in viewModel
            DataContext = _viewModel;

            // Bind to the PropertyChanged event to handle changes
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            // Attach a handler for the Closing event to detach event handlers and handle cleanup
            this.Closing += (s, e) =>
            {
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            };

            // Load the email from the viewModel
            LoadEmail(_viewModel.SelectedEmail);
            LoadCategories(); 

            if (!(_viewModel.SelectedEmail.IsDraft ))
            {
                subjectTextBox.IsReadOnly = true;
                contentTextBox.IsReadOnly = true;
                recipientsListBox.IsEnabled = false;

                lstAttachments.IsEnabled = false; 
                btnAddAttachment.IsEnabled = false;
                    btnSave.IsEnabled = false;
                    btnCancel.IsEnabled = false; 

            }
        }
   

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EmailClientViewModel.SelectedEmail))
            {
                LoadEmail(_viewModel.SelectedEmail);
            }
        }

        public void LoadEmail(Email email)
        {
            if (email == null) return;

            // Initialize TemporaryEmail with a new instance
            _temporaryEmail = new Email
            {
                Sender = email.Sender,
                Recipients = new ObservableCollection<string>(email.Recipients),
                Subject = email.Subject,
                Content = email.Content,
                Attachments = new ObservableCollection<string>(email.Attachments),
                Category = email.Category
            };

            // Update UI elements with the email details
            subjectTextBox.Text = _temporaryEmail.Subject;
            contentTextBox.Text = _temporaryEmail.Content;

            recipientsListBox.ItemsSource = _temporaryEmail.Recipients;
            lstAttachments.ItemsSource = _temporaryEmail.Attachments;
            cmbCategories.SelectedItem = email.Category; 
        }

        private void btnAddAttachment_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    _temporaryEmail.Attachments.Add(filename);
                }
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            var email = _viewModel.SelectedEmail;

            if (email != null)
            {
                // Update the email object with the UI values
                email.Subject = subjectTextBox.Text;
                email.Content = contentTextBox.Text;

                email.Recipients = new ObservableCollection<string>(recipientsListBox.Items.Cast<string>());
                email.Attachments = new ObservableCollection<string>(_temporaryEmail.Attachments);
                email.Category = _temporaryEmail.Category; 
                this.Close();
            }
        }

        private void CmbCategories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_temporaryEmail != null)
                _temporaryEmail.Category = cmbCategories.SelectedItem as string;
        }
        public ObservableCollection<string> Categories { get; set; } 
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
            cmbCategories.ItemsSource = Categories; 

        }

 
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void EmailEditWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadEmail(_viewModel.SelectedEmail);
        }

      
    }
}
 