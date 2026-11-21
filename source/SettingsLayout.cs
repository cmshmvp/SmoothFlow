using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SmoothFlow {
internal sealed partial class MainForm {
    readonly Dictionary<Control,string> captions=new Dictionary<Control,string>();
    readonly ComboBox language=new ComboBox();
    readonly List<string> languageIds=new List<string>{"auto"};
    readonly Label languageLabel=new Label();
    readonly Button add=new Button(), help=new Button(), reset=new Button();
    readonly ToolStripMenuItem fineSteps=new ToolStripMenuItem();
    static readonly Color ink=Color.FromArgb(31,54,52);
    static readonly Color railColor=Color.FromArgb(24,58,54);
    static readonly Color mint=Color.FromArgb(163,231,205);

    T Caption<T>(T control,string key) where T:Control {
        captions.Add(control,key);
        control.Text=UiText.Get(key);
        return control;
    }

    static TableLayoutPanel Grid(int columns, params int[] heights) {
        var grid=new TableLayoutPanel{ColumnCount=columns,RowCount=heights.Length,
            Dock=DockStyle.Fill,Margin=Padding.Empty,Padding=Padding.Empty};
        foreach(int height in heights)
            grid.RowStyles.Add(height<0?new RowStyle(SizeType.Percent,100):new RowStyle(SizeType.Absolute,height));
        return grid;
    }

    Label Heading(string key,float size) {
        return Caption(new Label{Dock=DockStyle.Fill,Margin=Padding.Empty,TextAlign=ContentAlignment.MiddleLeft,
            Font=new Font(Font.FontFamily,size,FontStyle.Bold)},key);
    }

    void StyleButton(Button button,string key,bool accent) {
        Caption(button,key);
        button.AutoSize=true;
        button.AutoSizeMode=AutoSizeMode.GrowAndShrink;
        button.Anchor=AnchorStyles.Left;
        button.Padding=new Padding(9,3,9,3);
        button.Margin=new Padding(6,0,0,0);
        button.FlatStyle=FlatStyle.Flat;
        button.FlatAppearance.BorderColor=Color.FromArgb(179,201,193);
        button.BackColor=accent?mint:Color.White;
        button.ForeColor=ink;
    }

    void BuildLayout() {
        BackColor=Color.FromArgb(242,246,243);
        ForeColor=ink;
        var shell=Grid(2,-1);
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,250));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        Controls.Add(shell);
        var rail=Grid(1,52,54,26,32,37,37,-1,28,40,48);
        rail.Padding=new Padding(22,24,22,14);
        rail.BackColor=railColor;
        rail.ForeColor=Color.White;
        shell.Controls.Add(rail,0,0);
        var mark=new PictureBox{SizeMode=PictureBoxSizeMode.Zoom,
            Size=new Size(46,46),Anchor=AnchorStyles.Left,Margin=Padding.Empty};
        using(var stream=typeof(MainForm).Assembly.GetManifestResourceStream("SmoothFlow.Brand.png"))
        using(var bitmap=new Bitmap(stream))mark.Image=new Bitmap(bitmap);
        mark.Disposed+=delegate{mark.Image.Dispose();};
        rail.Controls.Add(mark,0,0);
        rail.Controls.Add(new Label{Text="SmoothFlow",Dock=DockStyle.Fill,Margin=Padding.Empty,
            Font=new Font(Font.FontFamily,22F,FontStyle.Bold),TextAlign=ContentAlignment.MiddleLeft},0,1);
        var general=Heading("General",10F);
        general.ForeColor=mint;
        rail.Controls.Add(general,0,3);
        AddCheck(rail,4,globalEnabled,"Enabled");
        AddCheck(rail,5,autoStart,"AutoStart");
        Caption(languageLabel,"LanguageLabel");
        languageLabel.Dock=DockStyle.Fill;
        languageLabel.ForeColor=mint;
        languageLabel.TextAlign=ContentAlignment.MiddleLeft;
        rail.Controls.Add(languageLabel,0,7);
        language.DropDownStyle=ComboBoxStyle.DropDownList;
        language.Anchor=AnchorStyles.Left|AnchorStyles.Right;
        language.Margin=Padding.Empty;
        language.Items.Add(UiText.Get("SystemLanguage"));
        foreach(var choice in UiText.Choices){languageIds.Add(choice.Key);language.Items.Add(choice.Value);}
        language.SelectedIndex=Math.Max(0,languageIds.IndexOf(settings.InterfaceLanguage));
        rail.Controls.Add(language,0,8);
        StyleButton(help,"Help",false);
        help.Margin=Padding.Empty;
        help.BackColor=railColor;help.ForeColor=Color.White;
        rail.Controls.Add(help,0,9);

        var body=Grid(1,45,43,20,-1,51,36);
        body.Padding=new Padding(22,15,22,6);
        shell.Controls.Add(body,1,0);
        body.Controls.Add(Heading("Profile",15F),0,0);
        var picker=Grid(3,-1);
        picker.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        picker.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        picker.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        profiles.DropDownStyle=ComboBoxStyle.DropDownList;
        profiles.Anchor=AnchorStyles.Left|AnchorStyles.Right;
        profiles.Margin=Padding.Empty;
        picker.Controls.Add(profiles,0,0);
        StyleButton(add,"Add",true);StyleButton(remove,"Remove",false);
        picker.Controls.Add(add,1,0);picker.Controls.Add(remove,2,0);
        body.Controls.Add(picker,0,1);

        var card=Grid(1,42,114,12,31,31,31,31,-1);
        card.BackColor=Color.White;
        card.Padding=new Padding(18,5,18,6);
        body.Controls.Add(card,0,3);
        card.Controls.Add(Heading("Motion",16F),0,0);
        var values=Grid(3,38,38,38);
        values.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        values.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,90));
        values.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,38));
        card.Controls.Add(values,0,1);
        AddValue(values,0,"Distance",step,.25M,4,2,.05M,"×");
        AddValue(values,1,"Duration",duration,80,600,0,1,"ms");
        AddValue(values,2,"AccelMax",accelerationMax,1,3,1,.1M,"×");
        var rule=new Panel{Dock=DockStyle.Top,Height=1,Margin=new Padding(0,5,0,0),BackColor=BackColor};
        card.Controls.Add(rule,0,2);
        AddCheck(card,3,easing,"Easing");AddCheck(card,4,horizontal,"Horizontal");
        AddCheck(card,5,reverse,"Reverse");AddCheck(card,6,defaultEnabled,"AllApps");
        StyleButton(reset,"Reset",false);
        reset.Margin=Padding.Empty;
        body.Controls.Add(reset,0,4);
        status.Dock=DockStyle.Fill;status.Margin=Padding.Empty;
        status.Font=new Font(Font.FontFamily,9F);status.TextAlign=ContentAlignment.MiddleLeft;
        body.Controls.Add(status,0,5);
    }

    void AddValue(TableLayoutPanel grid,int row,string key,NumericUpDown number,decimal min,
                  decimal max,int decimals,decimal increment,string unit) {
        grid.Controls.Add(Caption(new Label{Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,
            Margin=Padding.Empty},key),0,row);
        number.Minimum=min;number.Maximum=max;number.DecimalPlaces=decimals;number.Increment=increment;
        number.Dock=DockStyle.None;number.Anchor=AnchorStyles.Left|AnchorStyles.Right;
        number.TextAlign=HorizontalAlignment.Center;
        grid.Controls.Add(number,1,row);
        grid.Controls.Add(new Label{Text=unit,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},2,row);
    }

    void AddCheck(TableLayoutPanel grid,int row,CheckBox box,string key) {
        Caption(box,key);
        box.Anchor=AnchorStyles.Left;box.AutoSize=true;box.Margin=Padding.Empty;
        grid.Controls.Add(box,0,row);
    }

    void Translate() {
        foreach(var entry in captions)entry.Key.Text=UiText.Get(entry.Value);
        int choice=language.SelectedIndex;
        updating=true;
        language.Items[0]=UiText.Get("SystemLanguage");
        language.SelectedIndex=choice;
        updating=false;
        language.AccessibleName=languageLabel.Text;
        profiles.AccessibleName=UiText.Get("Profile");
        step.AccessibleName=UiText.Get("Distance"); duration.AccessibleName=UiText.Get("Duration");
        accelerationMax.AccessibleName=UiText.Get("AccelMax");
        tips.SetToolTip(step,UiText.Get("DistanceTip"));
        add.AccessibleName=UiText.Get("AddApp");remove.AccessibleName=UiText.Get("RemoveApp");
        var items=tray.ContextMenuStrip.Items;
        fineSteps.Text=UiText.Get("Fine");
        items[1].Text=UiText.Get("Open");items[2].Text=UiText.Get("Toggle");items[3].Text=UiText.Get("Quit");
        ReloadProfiles(Math.Max(0,profiles.SelectedIndex));
        RefreshStatus();
    }
}
}
