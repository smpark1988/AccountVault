namespace AccountVault.Mobile.Pages;

public static class MobileLayout
{
    public static View CreateAuthLayout(string title, string description, IEnumerable<View> content)
    {
        var stack = new VerticalStackLayout
        {
            Padding = new Thickness(24),
            Spacing = 14,
            VerticalOptions = LayoutOptions.Center
        };

        stack.Children.Add(new Label
        {
            Text = title,
            FontSize = 28,
            FontAttributes = FontAttributes.Bold
        });
        stack.Children.Add(new Label
        {
            Text = description,
            TextColor = Color.FromArgb("#667085"),
            Margin = new Thickness(0, 0, 0, 10)
        });

        foreach (var view in content)
        {
            stack.Children.Add(view);
        }

        return new ScrollView { Content = stack };
    }

    public static View CreateField(string label, View content)
    {
        return new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                new Label
                {
                    Text = label,
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#667085")
                },
                content
            }
        };
    }
}
