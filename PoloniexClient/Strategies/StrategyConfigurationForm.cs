﻿using Crypto.Core.Strategies;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.DXErrorProvider;
using DevExpress.XtraLayout;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CryptoMarketClient.Strategies {
    public partial class StrategyConfigurationForm : XtraForm {

        StrategyBase strategy;

        StrategySpecificConfigurationControlBase strategySpecificSettingsControl;

        public StrategyConfigurationForm() {
            InitializeComponent();

            this.accountInfoBindingSource.DataSource = GetAccounts();
        }

        protected BindingList<AccountInfo> GetAccounts() {
            BindingList<AccountInfo> res = new BindingList<AccountInfo>();
            foreach(Exchange e in Exchange.Registered) {
                foreach(var account in e.Accounts) {
                    if(account.Active)
                        res.Add(account);
                }
            }
            return res;
        }

        private void simpleButton1_Click(object sender, EventArgs e) {
            if(!ValidateCore())
                return;
            DialogResult = DialogResult.OK;
            Close();
        }

        private bool ValidateCore() {
            this.dxErrorProvider1.ClearErrors();
            List<StrategyValidationError> list = Strategy.Validate();
            if(list.Count == 0)
                return true;
            SetValidationErrors(list);
            ShowValidationErrors(list);
            return false;
        }

        void ShowValidationErrors(List<StrategyValidationError> list) {
            StrategyValidationErrorsForm form = new StrategyValidationErrorsForm();
            form.Errors = list;
            form.Show();
        }

        void SetValidationErrors(List<StrategyValidationError> list) {
            this.dxErrorProvider1.ClearErrors();
            foreach(var error in list) {
                Control c = GetControlBindedTo(error.PropertyName, error.DataObject);
                if(c != null)
                    this.dxErrorProvider1.SetError(c, error.Description, ErrorType.Default);
            }
        }

        Control GetControlBindedTo(string propertyName, object dataObject) {
            return GetControlBindedTo(this, propertyName, dataObject);
        }

        private Control GetControlBindedTo(Control control, string propertyName, object dataObject) {
            if(control.DataBindings.Count > 0 && control.DataBindings[0].BindingMemberInfo.BindingMember == propertyName) {
                object source = control.DataBindings[0].DataSource;
                if(source == dataObject)
                    return control;
                BindingSource bs = source as BindingSource;
                if(bs != null && bs.DataSource == dataObject)
                    return control;
            }
            foreach(Control child in control.Controls) {
                Control found = GetControlBindedTo(child, propertyName, dataObject);
                if(found != null)
                    return found;
            }
            return null;
        }

        private void simpleButton2_Click(object sender, EventArgs e) {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        protected virtual void OnStrategyChanged() {
            this.statisticalArbitrageStrategyBindingSource.DataSource = Strategy;
            if(StrategySpecificSettingsControl != null)
                StrategySpecificSettingsControl.Strategy = Strategy;
            Text = Strategy.TypeName + " - Configuration";
            if(string.IsNullOrEmpty(Strategy.Name))
                Strategy.Name = Strategy.TypeName;
        }

        protected virtual void OnStrategySettingsControlChanged() {
            if(StrategySpecificSettingsControl != null)
                StrategySpecificSettingsControl.Strategy = Strategy;
            else
                return;
            LayoutControlItem item = this.lcgStrategySpecific.AddItem();
            this.dataLayoutControl1.Controls.Add(StrategySpecificSettingsControl);
            item.Control = StrategySpecificSettingsControl;
        }

        public StrategyBase Strategy {
            get { return strategy; }
            set {
                if(Strategy == value)
                    return;
                strategy = value;
                OnStrategyChanged();
            }
        }
        public StrategySpecificConfigurationControlBase StrategySpecificSettingsControl {
            get { return strategySpecificSettingsControl; }
            set {
                if(StrategySpecificSettingsControl == value)
                    return;
                strategySpecificSettingsControl = value;
                OnStrategySettingsControlChanged();
            }
        }

        private void textEdit1_TextChanged(object sender, EventArgs e) {
            
        }
    }
}
