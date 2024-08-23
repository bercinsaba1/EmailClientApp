using System;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Erasmus; // Assuming Erasmus is a namespace you have defined elsewhere in your project
using System.ComponentModel;
using System.Windows.Controls;
using System.Xml.Serialization;
using System.IO;
using System.Timers;
using System.Windows.Input; 


namespace Erasmus
{

    public class ViewModelFactory
    {
        private static SettingsViewModel _settingsViewModel;

        public static SettingsViewModel GetSettingsViewModel(EmailClientViewModel emailClientViewModel)
        {
            if (_settingsViewModel == null)
            {
                _settingsViewModel = new SettingsViewModel(emailClientViewModel);
            }
            return _settingsViewModel;
        }
    }
 


    public interface IExportService
    {
        void ExportData(string path);
    }

    public class ExportService : IExportService
    {
        public ObservableCollection<EmailFolder> PersonalFolders { get; set; }
        public ObservableCollection<EmailFolder> WorkFolders { get; set; }

        public ExportService(ObservableCollection<EmailFolder> personalFolders, ObservableCollection<EmailFolder> workFolders)
        {
            PersonalFolders = personalFolders;
            WorkFolders = workFolders;
        }

        public void ExportData(string path)
        {
            if (!PersonalFolders.Any() && !WorkFolders.Any())
            {
                MessageBox.Show("There are no folders to export.", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "XML Files (*.xml)|*.xml",
                Title = "Save Export File",
                FileName = path
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var allFolders = new ObservableCollection<EmailFolder>(PersonalFolders.Concat(WorkFolders));

                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<EmailFolder>));
                    using (StreamWriter writer = new StreamWriter(saveFileDialog.FileName))
                    {
                        serializer.Serialize(writer, allFolders);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export data: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


    }
 




    public class SettingsViewModel : INotifyPropertyChanged
    {
        private bool _canSetAutoSave; 

        public bool CanSetAutoSave
        {
            get => _canSetAutoSave;
            set
            {
                if (_canSetAutoSave != value)
                {
                    _canSetAutoSave = value;
                    OnPropertyChanged(nameof(CanSetAutoSave));
                    Properties.Settings.Default.CanSetAutoSave = value;
                    Properties.Settings.Default.Save();
                }
            }
        }
        private bool _canDisableAutoSave;

        public bool CanDisableAutoSave
        {
            get => _canDisableAutoSave;
            set
            {
                if (_canDisableAutoSave != value)
                {
                    _canDisableAutoSave = value;
                    OnPropertyChanged(nameof(CanDisableAutoSave));
                    Properties.Settings.Default.CanDisableAutoSave = value;
                    Properties.Settings.Default.Save();
                }
            }
        }

 
        public ObservableCollection<string> Categories { get; set; }
        public RelayCommand AddCategoryCommand { get; private set; }
        public RelayCommand DeleteCategoryCommand { get; private set; }
        public RelayCommand EditCategoryCommand { get; private set; }
        public ICommand ExportCommand { get; private set; }

        public ICommand BackUpCommand { get; private set; }


        private readonly EmailClientViewModel emailClientViewModel;
        private Timer autoSaveTimer;
        public ICommand TriggerBackupCommand { get; private set; }
        public ICommand DisableAutoSaveCommand { get; private set; }

        // AutoSaveInterval saniye cinsinden süreyi belirtir
        private int _autoSaveInterval;
        public int AutoSaveInterval
        {
            get => _autoSaveInterval;
            set
            {
                if (_autoSaveInterval != value)
                {
                    _autoSaveInterval = value;
                    OnPropertyChanged(nameof(AutoSaveInterval));
                    ConfigureAutoSaveTimer(); // Ensure the timer is reconfigured with the new interval
                }
            }
        }
 
        public SettingsViewModel(EmailClientViewModel emailClientViewModel)
        {

           
            this.emailClientViewModel = emailClientViewModel;
            LoadSettings(); 
            TriggerBackupCommand = new RelayCommand(ExecuteBackup, _ => CanSetAutoSave);

            DisableAutoSaveCommand = new RelayCommand(DisableAutoSave); 


           //  AutoSaveInterval = Properties.Settings.Default.AutoSaveInterval; 
         
            ConfigureAutoSaveTimer();
 
            //CanSetAutoSave = true; 

            if (Properties.Settings.Default.Categories == null)
            {
                // Initialize the Categories if it's null
                Properties.Settings.Default.Categories = new System.Collections.Specialized.StringCollection();
            }

            // Now it's safe to cast because we've ensured it's not null
            Categories = new ObservableCollection<string>(Properties.Settings.Default.Categories.Cast<string>()); Categories = new ObservableCollection<string>(Properties.Settings.Default.Categories.Cast<string>());
            AddCategoryCommand = new RelayCommand(AddCategory, CanAddCategory);
            DeleteCategoryCommand = new RelayCommand(DeleteCategory, CanModifyCategory);
            EditCategoryCommand = new RelayCommand(EditCategory, CanModifyCategory);
            SaveEditCommand = new RelayCommand(SaveEdit, CanSaveEdit);
            EditCategoryCommand = new RelayCommand(EditCategory, obj => !string.IsNullOrEmpty(EditInput) && SelectedCategory != null);

   
        }
        private void LoadSettings()
        {
            AutoSaveInterval = Properties.Settings.Default.AutoSaveInterval;
            CanSetAutoSave = Properties.Settings.Default.CanSetAutoSave;
            CanDisableAutoSave = Properties.Settings.Default.CanDisableAutoSave;
        }


        private void DisableAutoSave(object obj)
        {
            AutoSaveInterval = 0;  // This triggers the timer configuration


            CanSetAutoSave = true;
            CanDisableAutoSave = false;

            OnPropertyChanged(nameof(AutoSaveInterval));
            OnPropertyChanged(nameof(CanDisableAutoSave));
            OnPropertyChanged(nameof(CanSetAutoSave));
 

            SaveSettings(); // Optionally save settings here if needed immediately
            MessageBox.Show("Successfully disabled auto-backup");
        }

        private void ConfigureAutoSaveTimer()
        {
            if (autoSaveTimer != null)
            {
                autoSaveTimer.Stop();
                autoSaveTimer.Dispose();
            }

            if (AutoSaveInterval > 0)
            {
                autoSaveTimer = new Timer(AutoSaveInterval * 1000);
                autoSaveTimer.Elapsed += OnAutoSaveTimerElapsed;
                autoSaveTimer.AutoReset = true;
                autoSaveTimer.Start();
            }
        }



        private void OnAutoSaveTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (AutoSaveInterval > 0) // Use ViewModel property for consistency
            {
                ExecuteBackup(this);
            }
            else
            {
                Debug.WriteLine("Auto-save is currently disabled, skipping backup.");
            }
        }
 
        private void ExecuteBackup(object obj)
        {
            CanSetAutoSave = false;
            CanDisableAutoSave = true;

            if (emailClientViewModel.BackUpCommand.CanExecute(null))
            {
                emailClientViewModel.BackUpCommand.Execute(null);
            }
        }
 

        public void SaveSettings()
        {
            // Save individual settings
            Properties.Settings.Default.AutoSaveInterval = AutoSaveInterval;
            Properties.Settings.Default.CanSetAutoSave = CanSetAutoSave;
            Properties.Settings.Default.CanDisableAutoSave = CanDisableAutoSave; 



            // Save categories
            Properties.Settings.Default.Categories.Clear();
            foreach (var category in Categories)
            {
                Properties.Settings.Default.Categories.Add(category);
            }

            // Don't forget to save the changes
            Properties.Settings.Default.Save();
        } 



        public RelayCommand SaveEditCommand { get; private set; } 
        private string _selectedCategory;
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged(nameof(SelectedCategory));
            
                DeleteCategoryCommand.RaiseCanExecuteChanged();  // Assuming RelayCommand or similar that supports this
                EditCategoryCommand.RaiseCanExecuteChanged();
            }
        }


        private string _editInput;
        public string EditInput
        {
            get => _editInput;
            set
            {
                if (_editInput != value)
                {
                    _editInput = value;
                    OnPropertyChanged(nameof(EditInput));
                }
            }
        }
  
 
        private bool CanSaveEdit(object arg)
        {
            return !string.IsNullOrEmpty(SelectedCategory);
        }

        private void SaveEdit(object obj)
        {
            int index = Categories.IndexOf(SelectedCategory);
            if (index != -1)
            {
                Categories[index] = SelectedCategory;
                SaveCategories();
            }
        } 
        private bool CanAddCategory(object arg)
        {
            return !string.IsNullOrWhiteSpace(SelectedCategory) && !Categories.Contains(SelectedCategory);
        }

        private bool CanModifyCategory(object arg)
        {
            return SelectedCategory != null && Categories.Contains(SelectedCategory);
        }

        public void AddCategory(object obj)
        {
            Categories.Add(SelectedCategory);
            SaveCategories();
            SelectedCategory = null;
        }
public void DeleteCategory(object obj)
        {
            if (Categories.Contains(SelectedCategory))
            {
                Categories.Remove(SelectedCategory);
                SaveCategories();
                SelectedCategory = null;
            }
        }

        public void EditCategory(object obj)
        {
            int index = Categories.IndexOf(SelectedCategory);
            if (index != -1 && !string.IsNullOrEmpty(EditInput))
            {
                Categories[index] = EditInput; // Use EditInput to update the category
                SaveCategories();
                EditInput = ""; // Clear the edit box after updating
            }
        }
 
        public void SaveCategories()
        {
            Properties.Settings.Default.Categories.Clear();
            foreach (var category in Categories)
            {
                Properties.Settings.Default.Categories.Add(category);
            }
            Properties.Settings.Default.Save();
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
 

    public partial class SettingsWindow : Window
    {
        public SettingsViewModel ViewModel { get; }

        private ICommand BackUpCommand;


        public SettingsWindow(EmailClientViewModel emailClientViewModel) 
        {
            InitializeComponent();
            ViewModel = ViewModelFactory.GetSettingsViewModel(emailClientViewModel); 
            DataContext = ViewModel;
            this.Closing += SettingsWindow_Closing; // Subscribe to the Closing event 



        }


        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            DataContext = null;
            DataContext = ViewModel; // Reset DataContext to refresh bindings
        }
 
        private void SettingsWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ViewModel.SaveSettings(); // Call a method on the ViewModel to save the settings
            this.DataContext = null; 
        } 

        private void CategoriesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoriesListBox.SelectedItem != null)
            {
                ViewModel.SelectedCategory = CategoriesListBox.SelectedItem.ToString();
                // EditTextBox.Text = ViewModel.SelectedCategory; // Ensure TextBox shows selected category
            }
        }

        private void SaveEditButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectedCategory = box.Text; // Update ViewModel with new text
            ViewModel.SaveEditCommand.Execute(null); // Execute save edit command
        } 
        private void AddCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AddCategory(null);
        }

        private void DeleteCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (CategoriesListBox.SelectedItem != null)
            {
                ViewModel.DeleteCategory(CategoriesListBox.SelectedItem.ToString());
            }
        }

        private void EditCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.EditCategory(null); // You might pass null or a more relevant parameter based on your setup
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
 