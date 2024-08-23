using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Erasmus
{
    public partial class EmailSearchControl : UserControl
    {
        public EmailSearchControl()
        {
            InitializeComponent();
            txtSearchQuery.TextChanged += TxtSearchQuery_TextChanged;
            cmbSearchCategory.SelectionChanged += CmbSearchCategory_SelectionChanged; 
        }



        private void TxtSearchQuery_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSearch();
        }



        // Define an event in your user control
        public event Action<SearchEventArgs> PerformSearchRequested;



        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchEventArgs args = new SearchEventArgs
            {
                SearchText = txtSearchQuery.Text,
                SearchCategory = ((ComboBoxItem)cmbSearchCategory.SelectedItem).Content.ToString()
            };

            // Raise the event
            PerformSearchRequested?.Invoke(args);
        }
 


        private void CmbSearchCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSearch();
        } 

        // Event declaration using standard .NET event pattern
        public event EventHandler<SearchEventArgs> SearchChanged;

        protected virtual void OnSearchChanged(SearchEventArgs e)
        {
            SearchChanged?.Invoke(this, e);
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            txtSearchQuery.Clear();
            cmbSearchCategory.SelectedIndex = 0;
            OnSearchChanged(new SearchEventArgs { SearchText = "", SearchCategory = "Subject" });
        }

        // Call this method when search criteria are updated 
        public void UpdateSearch()
        {
            string searchText = txtSearchQuery.Text;
            string searchCategory = ((ComboBoxItem)cmbSearchCategory.SelectedItem).Content.ToString();
            OnSearchChanged(new SearchEventArgs { SearchText = searchText, SearchCategory = searchCategory });
        }
    }

    // Custom EventArgs to pass search data
    
}
 
 