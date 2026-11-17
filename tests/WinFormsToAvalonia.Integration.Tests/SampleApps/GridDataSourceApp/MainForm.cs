using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GridDataSourceApp.Models;

namespace GridDataSourceApp
{
    public partial class MainForm : Form
    {
        private readonly List<Customer> customers = new List<Customer>
        {
            new Customer { Name = "Ada", Age = 36 },
            new Customer { Name = "Grace", Age = 45 },
        };

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // No columns of its own: WinForms generates them from the row type.
            autoGrid.DataSource = customers;

            // Designer columns, generation off: only the bound column shows.
            boundGrid.DataSource = customers.ToArray();
        }

        private void lockButton_Click(object sender, EventArgs e)
        {
            boundGrid.ReadOnly = true;
            autoGrid.DataSource = null;
            autoGrid.DataSource = customers;
        }
    }
}
