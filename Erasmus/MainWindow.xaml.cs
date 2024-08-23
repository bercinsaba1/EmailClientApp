using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using System.Windows.Data;

namespace Erasmus
{

    [XmlRoot("Folders")]
    public class FolderCollection
    {
        [XmlElement("EmailFolder")]
        public ObservableCollection<EmailFolder> EmailFolders { get; set; }
    }




    [XmlRoot("Email")]
    public class Email : INotifyPropertyChanged
    {
        private string category;
        public string Category
        {
            get => category;
            set
            {
                if (category != value)
                {
                    category = value;
                    OnPropertyChanged(nameof(Category));
                }
            }
        }
        public bool IsDraft { get; set; }
        public bool IsDeleted { get; set; }
        public string Sender { get; set; }
        // public List<string> Recipients { get; set; } = new List<string>(); GEREKIRSE LIST YAP 
        public List<string> CCRecipients { get; set; } = new List<string>();
        private string subject;
        private ObservableCollection<string> recipients;
        private string content;

        public ObservableCollection<string> Recipients
        {
            get => recipients;
            set
            {
                if (recipients != value)
                {
                    recipients = value;
                    OnPropertyChanged(nameof(Recipients));
                }
            }
        }

        public string Subject
        {
            get => subject;
            set
            {
                subject = value;
                OnPropertyChanged(nameof(Subject));
            }
        }
 

        public string Content
        {
            get => content;
            set
            {
                content = value;
                OnPropertyChanged(nameof(Content));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // public string Content { get; set; }
        // public List<string> Attachments { get; set; } = new List<string>();
        private ObservableCollection<string> _attachments;
        public ObservableCollection<string> Attachments
        {
            get => _attachments;
            set
            {
                _attachments = value;
                OnPropertyChanged(nameof(Attachments));
            }
        } 


        public DateTime DateSent { get; set; }
        public bool IsRead { get; set; }
        public Email()
        {
            Recipients = new ObservableCollection<string>();
            CCRecipients = new List<string>();
            Attachments = new ObservableCollection<string>(new List<string>());
 
        }

        public Email(string sender, ObservableCollection<string> recipients, string subject, string content, List<string> attachments, String Category)
        {
            Sender = ValidateEmail(sender) ? sender : throw new ArgumentException("Invalid sender email address");
            Recipients = recipients;
            Subject = subject;
            Content = content;
            if (attachments != null)
            {
                Attachments = new ObservableCollection<string>(attachments);
            }
            else
            {
                Attachments = new ObservableCollection<string>();
            }
 

            DateSent = DateTime.Now; //    current time
            category = Category;
        }

        private bool ValidateEmail(string email)
        {
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }
    }

    public class EmailFolder
    {


        [XmlElement("Email")]
        public List<Email> Emails { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Name { get; set; }
        public List<EmailFolder> SubFolders { get; set; } = new List<EmailFolder>();
        public ObservableCollection<Email> Messages { get; set; } = new ObservableCollection<Email>();


        [XmlIgnore]
        public ObservableCollection<Email> FilteredMessages { get; private set; }
        public Email SelectedEmail { get; set; }


        public EmailFolder()
        {
            SubFolders = new List<EmailFolder>();
            Messages = new ObservableCollection<Email>();
            Emails = new List<Email>();
        }


        public EmailFolder(string name)
        {
            Name = name;
            Emails = new List<Email>();
            SubFolders = new List<EmailFolder>();
        }

        public void AddEmail(Email email)
        {
            Messages.Add(email);
        }
        public void AddSubFolder(EmailFolder folder)
        {
            SubFolders.Add(folder);
        }



    }

    // MY VIEW MODEL 
    public class EmailClientViewModel : INotifyPropertyChanged
    {

        public bool IsEditEnabled => SelectedEmail?.IsDraft ?? false;
 
        public ICommand AddEmailCommand { get; private set; }
        private EmailEditWindow editWindow;

        public ICommand AttachFileCommand { get; private set; }
        public ICommand SaveCommand { get; set; }
        public ICommand ResetSearchCommand { get; private set; }
        public ICommand SearchCommand { get; private set; }
        public ICommand CloseCommand { get; set; }
 

        private string _searchQuery;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (_searchQuery != value)
                {
                    _searchQuery = value;
                    OnPropertyChanged(nameof(SearchQuery));
                    PerformSearch();
                }
            }
        }

        public void PerformSearch()
        {
            var allFolders = new ObservableCollection<EmailFolder>(PersonalFolders.Concat(WorkFolders));

            if (string.IsNullOrEmpty(SearchQuery) || !allFolders.Any())
            {
                FilteredEmails = new ObservableCollection<Email>();
            }
            else
            {
 
                var allMessages = allFolders.SelectMany(folder => folder.Messages);

          
                IEnumerable<Email> queryResults = Enumerable.Empty<Email>();

                switch (SelectedSearchCategory)
                {
                    case "Subject":
                        queryResults = allMessages.Where(email => email.Subject.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
                        break;
                    case "Sender":
                        queryResults = allMessages.Where(email => email.Sender.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
                        break;
                    case "Recipient":
                        queryResults = allMessages.Where(email => email.Recipients.Any(r => r.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)));
                        break;
                }

                FilteredEmails = new ObservableCollection<Email>(queryResults);
            }
        }


        private string _selectedSearchCategory = "Subject";
        public string SelectedSearchCategory
        {
            get => _selectedSearchCategory;
            set
            {
                if (_selectedSearchCategory != value)
                {
                    _selectedSearchCategory = value;
                    OnPropertyChanged(nameof(SelectedSearchCategory));
                    PerformSearch();
                }
            }
        }


        private ObservableCollection<Email> _filteredEmails;
        public ObservableCollection<Email> FilteredEmails
        {
            get => _filteredEmails;
            set
            {
                _filteredEmails = value;
                OnPropertyChanged(nameof(FilteredEmails));
            }
        }

        private EmailFolder _selectedFolder;

     

     private Email _selectedEmail;
        public Email SelectedEmail
        {
            get => _selectedEmail;
            set
            {
                if (_selectedEmail != value)
                {
                    _selectedEmail = value;
                    OnPropertyChanged(nameof(SelectedEmail));
                    OnPropertyChanged(nameof(IsEditEnabled));
                    UpdateEditWindow();
                }
            }
        }
   

        public void FilterMessages(string searchText, string category)
        {
            if (category == "Subject" && !string.IsNullOrWhiteSpace(searchText))
            {
                var filtered = Messages.Where(email => email.Subject.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();
                FilteredMessages.Clear();
                foreach (var email in filtered)
                    FilteredMessages.Add(email);
            }
            else
            {
                FilteredMessages = new ObservableCollection<Email>(Messages);
            }


        }
        public Email GetSelectedEmail()
        {
            return SelectedEmail;
        }


        private EmailEditWindow emailEditWindow;
        private string _selectedMailboxType;
        // private EmailFolder _selectedFolder;
        private void OnSelectedEmailChanged()
        {
            if (emailEditWindow != null && !emailEditWindow.IsVisible)
            {
                emailEditWindow.DataContext = this.SelectedEmail;
            }
        }

        public EmailFolder SelectedFolder
        {
            get => _selectedFolder;
            set
            {
                _selectedFolder = value;
                OnPropertyChanged(nameof(SelectedFolder));
                OnPropertyChanged(nameof(Messages)); 
            }
        }

        public void UpdateSelectedEmail()
        {
     
            OnPropertyChanged(nameof(SelectedEmail));
        }


        public string SelectedMailboxType
        {
            get => _selectedMailboxType;
            set
            {
                _selectedMailboxType = value;
                OnPropertyChanged(nameof(SelectedMailboxType));
            }
        }
        public EmailFolder FindFolder(string folderName)
        {
            return PersonalFolders.Concat(WorkFolders).FirstOrDefault(f => f.Name == folderName);
        }
        private ObservableCollection<EmailFolder> _personalFolders;
        public ObservableCollection<EmailFolder> PersonalFolders
        {
            get => _personalFolders;
            set
            {
                if (_personalFolders != value)
                {
                    _personalFolders = value;
                    OnPropertyChanged(nameof(PersonalFolders));
                }
            }
        }
        private ObservableCollection<EmailFolder> _workFolders;
        public ObservableCollection<EmailFolder> WorkFolders
        {
            get => _workFolders;
            set
            {
                if (_workFolders != value)
                {
                    _workFolders = value;
                    OnPropertyChanged(nameof(WorkFolders));
                }
            }
        } 

 
        EmailEditWindow? editWin = null;
       

        public ICommand ImportCommand { get; private set; }
        public ICommand ExportCommand { get; private set; }
        public ICommand BackUpCommand { get; private set; }

        private ObservableCollection<EmailFolder> _folders;
        public ObservableCollection<EmailFolder> Folders
        {
            get => _folders;
            set { _folders = value; OnPropertyChanged(nameof(Folders)); }
        }

        private ObservableCollection<Email> _messages;
        public ObservableCollection<Email> Messages
        {
            get => _messages;
            set
            {
                if (_messages != value)
                {
                    _messages = value;
                    OnPropertyChanged(nameof(Messages));
                }
            }
        }
      

 
        // commands 
        public ICommand ExitCommand { get; }
        public ICommand OpenSettingsCommand { get; private set; }

        public ICommand RemoveEmailCommand { get; private set; }
        public ICommand EditEmailCommand { get; private set; }
        public ICommand EmailSelectCommand { get; }


        // constructor of view model 
        private void InitializeFolders()
        {

            PersonalFolders = new ObservableCollection<EmailFolder>();
            WorkFolders = new ObservableCollection<EmailFolder>(); 
          
            var personalDrafts = new EmailFolder("Drafts");
            var personalSent = new EmailFolder("Sent");
            PersonalFolders.Add(personalDrafts);
            PersonalFolders.Add(personalSent);
 
            var workDrafts = new EmailFolder("Drafts");
            var workSent = new EmailFolder("Sent");
            WorkFolders.Add(workDrafts);
            WorkFolders.Add(workSent);
        }



        public ObservableCollection<Email> FilteredMessages { get; private set; }
        public EmailClientViewModel()
        {
            InitializeFolders();
            AddEmailCommand = new RelayCommand(OpenNewEmailDialog);
            Folders = new ObservableCollection<EmailFolder>();
            Messages = new ObservableCollection<Email>();

            FilteredMessages = new ObservableCollection<Email>(Messages);

            ExitCommand = new RelayCommand(_ =>
            {
                // Reset settings
                Properties.Settings.Default.AutoSaveInterval = 0;
                Properties.Settings.Default.CanSetAutoSave = true;
                Properties.Settings.Default.CanDisableAutoSave = false ;

                // Save the settings
                Properties.Settings.Default.Save();

                // Shutdown the application
                Application.Current.Shutdown();
            });  


            EmailSelectCommand = new RelayCommand(SelectEmail);

            RemoveEmailCommand = new RelayCommand(RemoveEmail, CanRemoveEmail);
            EditEmailCommand = new RelayCommand(EditEmail, CanRemoveEmail); // Later add CanEditEmail 
            ImportCommand = new RelayCommand(obj =>
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "XML Files (*.xml)|*.xml",
                    Title = "Open Export File"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    ImportEmails(openFileDialog.FileName);
                }
            });
            //ExportCommand = new RelayCommand(param => SaveEmailData(param.ToString()));

            ExportCommand = new RelayCommand(obj =>
            {
                if (!PersonalFolders.Any() && !WorkFolders.Any())
                {
                    MessageBox.Show("There are no folders to export.", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "XML Files (*.xml)|*.xml",
                    Title = "Save Export File"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    ExportEmails(saveFileDialog.FileName);
                }
            });

            BackUpCommand = new RelayCommand(obj =>
            {
                if (!PersonalFolders.Any() && !WorkFolders.Any())
                {
                    MessageBox.Show("There are no folders to export.", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }




                ExportEmails(GetDefaultFilePath());
                MessageBox.Show("It is sucesfully backupped"); 

            });



   
            AttachFileCommand = new RelayCommand(obj => AttachFile());
            OpenSettingsCommand = new RelayCommand(OpenSettings);

            SaveCommand = new RelayCommand(obj => SaveEmail(obj)); 
            CloseCommand = new RelayCommand(obj => CloseWindow(obj));  

            FilteredEmails = new ObservableCollection<Email>();
            SearchCommand = new RelayCommand(param => PerformSearch());
            ResetSearchCommand = new RelayCommand(param => ResetSearch());


        }

        public string GetDefaultFilePath()
        {
            string directory = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(directory, "projectfile.xml");
        }

 
        public void ImportEmails(string path)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<EmailFolder>));
                using (StreamReader reader = new StreamReader(path))
                {
                    ObservableCollection<EmailFolder> loadedFolders = (ObservableCollection<EmailFolder>)serializer.Deserialize(reader);
                    PersonalFolders.Clear();
                    foreach (EmailFolder folder in loadedFolders)
                    {
                        PersonalFolders.Add(folder);
                    }
                    OnPropertyChanged(nameof(PersonalFolders));
                    OnPropertyChanged(nameof(Messages));
                    OnPropertyChanged(nameof(SelectedEmail));

                    // Draft folder'ı seçmek için güncellenen kontrol
                    SelectedFolder = PersonalFolders.FirstOrDefault(f => f.Name == "Draft");

                    // Eğer Draft folder bulunursa, mesajlarını yükle, yoksa Messages'ı boş bırak
                    if (SelectedFolder != null)
                    {
                        Messages = new ObservableCollection<Email>(SelectedFolder.Messages);
                    }
                    else
                    {
                        Messages = new ObservableCollection<Email>();
                    }
                }

            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show($"Failed to load data: {ex.InnerException?.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load data: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void ExportEmails(string path)
        {
            var allFolders = new ObservableCollection<EmailFolder>(PersonalFolders.Concat(WorkFolders));

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<EmailFolder>));
                using (StreamWriter writer = new StreamWriter(path))
                {
                    serializer.Serialize(writer, allFolders);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export data: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AttachFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "All files (*.*)|*.*";
            openFileDialog.Multiselect = true; // Kullanıcının birden fazla dosya seçmesine izin ver

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    SelectedEmail.Attachments.Add(filename);  // Seçilen dosyaları Attachments listesine ekle
                }
            }
            OnPropertyChanged(nameof(SelectedEmail));  // UI'nın güncellenmesini tetikle
        }
        private void ResetSearch()
        {
            SearchQuery = string.Empty;  
            SelectedSearchCategory = "Subject";  
        }

        private void OpenEditEmailWindow(Email selectedEmail)
        {
             
            if (editWindow == null || !editWindow.IsVisible)
            {
                

                
                editWindow = new EmailEditWindow(this);  

                editWindow.Owner = Application.Current.MainWindow; 
                editWindow.Closed += (s, e) => editWindow = null;  

                // Set the DataContext to the selected email
                editWindow.DataContext = selectedEmail;
                editWindow.Show();
            }
            else
            {        
                editWindow.Focus();
                editWindow.Activate(); 
                editWindow.DataContext = null;  
                editWindow.DataContext = GetSelectedEmail();  
            }
        } 
         
        public void LoadEmailData(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(EmailFolder));
            using (StreamReader reader = new StreamReader(path))
            {
                EmailFolder loadedData = (EmailFolder)serializer.Deserialize(reader);
                PersonalFolders.Clear();
                PersonalFolders.Add(loadedData);
                OnPropertyChanged(nameof(PersonalFolders));
            }
        }
        public void SaveEmailData(string path)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(EmailFolder));
                using (StreamWriter writer = new StreamWriter(path))
                {
                    var folderToSave = PersonalFolders.FirstOrDefault();
                    if (folderToSave != null)
                    {
                        serializer.Serialize(writer, folderToSave);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                Debug.WriteLine("Failed to save data: " + ex.Message);
            }
        }


        private void UpdateEditWindow()
        {
            if (editWindow != null && editWindow.IsVisible)
            {
                editWindow.DataContext = null; // Reset DataContext to ensure UI updates
                editWindow.DataContext = SelectedEmail;
            }
        }
        private void SaveEmail(object parameter)
        {
            // Save the changes back to the database or model
        }

        private void CloseWindow(object parameter)
        {
            // Close the edit window
        }


        private void OpenNewEmailDialog(object obj)
        {
            NewEmailWindow newEmailWindow = new NewEmailWindow();
            newEmailWindow.Owner = Application.Current.MainWindow;

            // Show dialog and wait for user action
            if (newEmailWindow.ShowDialog() == true)
            {
                // Determine where to save the email based on whether it's a draft
                EmailFolder targetFolder = newEmailWindow.IsDraft ? FindDraftsFolder() : FindCurrentSelectedFolder();
                targetFolder?.Messages.Add(newEmailWindow.ResultEmail);
                OnPropertyChanged(nameof(Messages));
            }
        }
        /*
        public void AddEmail(Email email, bool showDialog = false)
        {
            if (showDialog)
            {
                NewEmailWindow newEmailWindow = new NewEmailWindow();
                newEmailWindow.Owner = Application.Current.MainWindow;
                if (newEmailWindow.ShowDialog() == true)
                {
                    email = newEmailWindow.ResultEmail; // Use the email created in the dialog
                }
            }
            EmailFolder targetFolder = FindAppropriateFolder(email);
            if (targetFolder != null)
            {
                targetFolder.Messages.Add(email);
                OnPropertyChanged(nameof(Messages));
            }
        }

        public EmailFolder FindAppropriateFolder(Email email)
        {
            if (email.IsDraft)
                return FindDraftsFolder();
            else
                return FindSentFolder();
        }*/ 

        private EmailFolder FindCurrentSelectedFolder()
        {
            // Example implementation, assuming you have a way to know the current selected folder's name
            string currentFolderName = "Sent"; // This should be dynamically set based on UI
            return PersonalFolders.Concat(WorkFolders).FirstOrDefault(folder => folder.Name == currentFolderName);
        }
        private EmailFolder FindDraftsFolder()
        {
            // It might be better to distinguish between personal and work drafts if necessary
            return PersonalFolders.Concat(WorkFolders).FirstOrDefault(folder => folder.Name == "Drafts");
        }
        /*
        private EmailFolder FindSentFolder()
        {
            // Similarly for Sent
            return PersonalFolders.Concat(WorkFolders).FirstOrDefault(folder => folder.Name == "Sent");
        }*/

        private SettingsViewModel _cachedViewModel;
 

        private void OpenSettings(object obj)
        {
            // Check if a cached ViewModel exists; if not, create a new one
            if (_cachedViewModel == null)
                _cachedViewModel = new SettingsViewModel(this);

            // Create a new SettingsWindow instance
            SettingsWindow settingsWindow = new SettingsWindow(this)
            {
                DataContext = _cachedViewModel,  // Set the DataContext to the cached ViewModel
                Owner = Application.Current.MainWindow // Set the owner if required for modal behavior
            };

            // Optionally decide how you want to display the window based on your needs:
            // settingsWindow.Show(); // For non-modal window
            settingsWindow.ShowDialog(); // For modal window, use one or the other, not both
        }
 
        public event PropertyChangedEventHandler PropertyChanged; 
        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (propertyName == nameof(SelectedEmail) && editWindow != null && editWindow.IsVisible)
            {
                UpdateEditWindow(); // Update edit window when selected email changes
            }
        }

 
        private void SelectEmail(object email)
        {
            SelectedEmail = email as Email;
        }


        private bool CanRemoveEmail(object parameter)
        {
            return SelectedEmail != null;
        }

        private void RemoveEmail(object parameter)
        {

            var allFolders = new ObservableCollection<EmailFolder>(PersonalFolders.Concat(WorkFolders));
            bool emailRemoved = false;

            foreach (var folder in allFolders)
            {
                if (folder.Messages.Contains(SelectedEmail))
                {
                    folder.Messages.Remove(SelectedEmail);
                    emailRemoved = true;
                    break; // Email bulunduğunda döngüyü kır
                }
            }

            if (emailRemoved)
            {
                MessageBox.Show("Email removed successfully.");
            }
            else
            {
                MessageBox.Show("Selected email not found in any folder.");
            }
        }
        // Assuming this method is in your main ViewModel or code-behind
        private void EditEmail(object parameter)
        {
            if (SelectedEmail != null)
            {
                OpenEditEmailWindow(SelectedEmail); 
            }
        }

    }

 
    public partial class MainWindow : Window
    {
        private EmailClientViewModel viewModel;
        private EmailEditWindow editWindow;


        public MainWindow()
        {
            InitializeComponent();
            viewModel = new EmailClientViewModel();
            DataContext = viewModel; // Doğru ViewModel nesnesini kullan
          
            viewModel.PropertyChanged += ViewModel_PropertyChanged;

            emailSearchControl.PerformSearchRequested += PerformSearchAndShowResults;
            SetDefaultLayout();
            viewModel.ImportEmails(viewModel.GetDefaultFilePath());

            // Set default values
            Properties.Settings.Default.AutoSaveInterval = 0;
            Properties.Settings.Default.CanSetAutoSave = true;
            Properties.Settings.Default.CanDisableAutoSave = false; // Assuming you want this disabled by default

            // Save the settings
            Properties.Settings.Default.Save(); 
        }
 


        private EmailEditWindow emailEditWindow;

        private void EmailSearchControl_SearchChanged(object sender, SearchEventArgs e)
        {
            viewModel.SearchQuery = e.SearchText;
            viewModel.SelectedSearchCategory = e.SearchCategory;
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                viewModel.LoadEmailData(openFileDialog.FileName);
            }
        }

        public void PerformSearchAndShowResults(SearchEventArgs args)
        {
            // Assuming your ViewModel has a method to handle the search and return results
            var viewModel = DataContext as EmailClientViewModel;
            if (viewModel != null)
            {
                viewModel.SearchQuery = args.SearchText;
                viewModel.SelectedSearchCategory = args.SearchCategory;
                viewModel.PerformSearch();

                // Create a message from the search results
                var results = viewModel.FilteredEmails;
                var resultMessage = string.Join("\n", results.Select(email =>
    $"Subject: {email.Subject}, Sender: {email.Sender}, Recipients: {string.Join(", ", email.Recipients)}"));
 

                // Show the results in a MessageBox
                MessageBox.Show(resultMessage, "Search Results", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                viewModel.SaveEmailData(saveFileDialog.FileName);
            }
        }

        private void ShowOrActivateEmailEditWindow()
        {
            if (editWindow == null || !editWindow.IsVisible)
            {
                editWindow = new EmailEditWindow(((EmailClientViewModel)DataContext));
                editWindow.DataContext = ((EmailClientViewModel)DataContext).SelectedEmail;
                editWindow.Closed += (s, e) => editWindow = null;
                editWindow.Show();
            }
            else
            {
                editWindow.Activate();
                editWindow.DataContext = null; // Reset DataContext to force update
                editWindow.DataContext = ((EmailClientViewModel)DataContext).SelectedEmail;
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EmailClientViewModel.SelectedEmail))
            {
                // Check if the edit window is already open
                if (editWindow != null && editWindow.IsVisible)
                {
                    // Update the DataContext of the edit window to reflect changes
                    editWindow.DataContext = null;  // Detach old data context
                    editWindow.DataContext = ((EmailClientViewModel)sender).SelectedEmail;   // Attach new data context
                    ShowOrActivateEmailEditWindow();
                }
            }
        }


        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AutoSaveInterval = 0;
            Properties.Settings.Default.CanSetAutoSave = true;
            Properties.Settings.Default.CanDisableAutoSave = true; 
            Properties.Settings.Default.Save(); 

            Application.Current.Shutdown();
        }

        private void MessagesListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is EmailClientViewModel viewModel && viewModel.SelectedEmail != null)
            {
                MessageBox.Show(viewModel.SelectedEmail.Subject, "Email Subject", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // for differnet categorization as personal or wprk 
        private TreeViewItem GetParentTreeViewItem(FrameworkElement item)
        {
            while (item != null && !(item is TreeViewItem))
            {
                item = VisualTreeHelper.GetParent(item) as FrameworkElement;
            }
            return item as TreeViewItem;
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var treeView = sender as TreeView;
            if (treeView?.SelectedItem is TreeViewItem selectedItem)
            {
                var viewModel = DataContext as EmailClientViewModel;
                if (viewModel != null)
                {
                    string folderName = selectedItem.Tag as string;
                    var parentItem = GetParentTreeViewItem(selectedItem);
                    string mailboxType = parentItem?.Header as string;

                    if (!string.IsNullOrEmpty(folderName) && mailboxType != null)
                    {
                        viewModel.SelectedFolder = viewModel.FindFolder(folderName);
                        viewModel.Messages = viewModel.SelectedFolder?.Messages;
                    }
                }
            }
        }

        private void EmailSearchControl_Loaded(object sender, RoutedEventArgs e)
        {

        }


        private void DefaultLayout_Click(object sender, RoutedEventArgs e)
        {
            SetDefaultLayout();
        }
        private void AlternativeLayout_Click(object sender, RoutedEventArgs e)
        {
            SetAlternativeLayout();
        }


        private void SetDefaultLayout()
        {
            var defaultLayout = new DefaultLayout();
            defaultLayout.DataContext = this.DataContext;
            MainContent.Content = defaultLayout;
        }


        private void SetAlternativeLayout()
        {
            var alternativeLayout = new AlternativeLayout();
            alternativeLayout.DataContext = this.DataContext;
            MainContent.Content = alternativeLayout;
        }

 
    }


}