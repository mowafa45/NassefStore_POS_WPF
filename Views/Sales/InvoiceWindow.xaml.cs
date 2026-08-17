using NassefStore.Data.Entities;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;

namespace NassefStore.Views.Sales;

public partial class InvoiceWindow : Window
{
    private readonly Sale _sale;

    public InvoiceWindow(Sale sale)
    {
        InitializeComponent();
        _sale = sale;
        LoadInvoice();
    }

    private void LoadInvoice()
    {
        InvoiceNo.Text = $"Invoice #: {_sale.InvoiceNumber}";
        InvoiceDate.Text = _sale.SaleDate.ToString("dd/MM/yyyy HH:mm");
        CustomerName.Text = _sale.Customer?.Name ?? "Walk-in Customer";
        CustomerPhone.Text = _sale.Customer?.Phone ?? "";
        PaymentMethodText.Text = _sale.PaymentMethod.ToString();

        InvoiceItemsGrid.ItemsSource = _sale.Items;

        TotalText.Text = $"EGP {_sale.TotalAmount:N2}";
        DiscountText.Text = $"EGP {_sale.DiscountAmount:N2}";
        NetText.Text = $"EGP {_sale.NetAmount:N2}";
        PaidText.Text = $"EGP {_sale.PaidAmount:N2}";

        if (_sale.RemainingAmount > 0)
        {
            RemainingText.Text = $"EGP {_sale.RemainingAmount:N2}";
            RemainingRow.Visibility = Visibility.Visible;
        }
        else RemainingRow.Visibility = Visibility.Collapsed;
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        var pd = new PrintDialog();
        if (pd.ShowDialog() == true)
        {
            InvoiceContent.Measure(new Size(pd.PrintableAreaWidth, double.PositiveInfinity));
            InvoiceContent.Arrange(new Rect(new Size(pd.PrintableAreaWidth, InvoiceContent.DesiredSize.Height)));
            pd.PrintVisual(InvoiceContent, $"Invoice {_sale.InvoiceNumber}");
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
