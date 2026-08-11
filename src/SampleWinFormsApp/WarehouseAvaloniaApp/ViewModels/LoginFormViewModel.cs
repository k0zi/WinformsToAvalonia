using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WarehouseApp.Common;
using WarehouseApp.Data.Data;

namespace WarehouseAvaloniaApp.ViewModels;

/// <summary>
/// ViewModel for LoginForm (user customizations).
/// This file is preserved during reconversion - add your custom code here.
/// </summary>
public partial class LoginFormViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    internal void SetBusy(bool busy)
        {
            loginProgressBar.Visible = busy;
            loadingSpinner.Spinning = busy;
            loginButton.Enabled = !busy;
            usernameTextBox.Enabled = !busy;
            passwordTextBox.Enabled = !busy;
        }

}
