using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data.Core;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimplePlayer.Entities;
using SimplePlayer.ViewModels;

namespace SimplePlayer.Models;

public partial class SoundBoardModel : ObservableObject
{
    [ObservableProperty] private string _name;

    [ObservableProperty] private int _boardColumns = 10;

    [ObservableProperty] private int _boardRows = 6;

    [ObservableProperty] private bool _isEditing = false;

    public ObservableCollection<SoundModel> Sounds { get; } = new();

    #region Funny

    public ItemsControl ButtonControl { get; } = new();
    public Grid ButtonGrid { get; } = new();

    #endregion

    public SoundBoardsViewModel Parent { get; set; }

    public SoundBoardModel(SoundBoardsViewModel parent, string name = "Untitled")
    {
        Parent = parent;
        Name = name;
        var path = new CompiledBindingPathBuilder();
        path.SetRawSource(Sounds);
        var propInfo = new ClrPropertyInfo("Sounds", o => Sounds, null, typeof(ObservableCollection<SoundModel>));
        path.Property(propInfo, PropertyInfoAccessorFactory.CreateInpcPropertyAccessor);
        var binding = new CompiledBindingExtension(path.Build());
        ButtonControl.Bind(ItemsControl.ItemsSourceProperty, binding);
        ButtonControl.ItemsPanel = new FuncTemplate<Panel?>(() =>
        {
            ButtonControl.Loaded += (sender, args) =>
            {
                ButtonGrid.ColumnDefinitions.Clear();
                ButtonGrid.RowDefinitions.Clear();
                for (var i = 0; i < BoardColumns; i++)
                    ButtonGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
                for (var i = 0; i < BoardRows; i++)
                    ButtonGrid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star));

                //Fill the grid with empty buttons, rows first then columns
                for (var i = 0; i < BoardRows; i++)
                {
                    for (var j = 0; j < BoardColumns; j++)
                    {
                        if (!Sounds.All(x => x.Column != j || x.Row != i))
                            continue;
                        var button = new SoundModel(this)
                        {
                            Column = j,
                            Row = i
                        };
                        Sounds.Add(button);
                    }
                }
            };
            return ButtonGrid;
        });
    }

    public SoundBoardModel(SoundBoardsViewModel parent, SoundBoard soundBoard)
    {
        Parent = parent;
        Name = soundBoard.Name;
        BoardColumns = soundBoard.Columns;
        BoardRows = soundBoard.Rows;
        Sounds = new(soundBoard.Sounds.Select(x => new SoundModel(this)
        {
            Column = x.Column,
            Row = x.Row,
            Loop = x.Loop,
            Fade = x.Fade,
            FadeDuration = x.FadeDuration,
            Volume = x.Volume,
            SelectedDevice = x.Device,
        }));
        //build the binding
        var path = new CompiledBindingPathBuilder();
        path.SetRawSource(Sounds);
        var propInfo = new ClrPropertyInfo("Sounds", o => Sounds, null, typeof(ObservableCollection<SoundModel>));
        path.Property(propInfo, PropertyInfoAccessorFactory.CreateInpcPropertyAccessor);
        var binding = new CompiledBindingExtension(path.Build());
        ButtonControl.Bind(ItemsControl.ItemsSourceProperty, binding);
        ButtonControl.ItemsPanel = new FuncTemplate<Panel?>(() =>
        {
            ButtonControl.Loaded += (sender, args) =>
            {
                ButtonGrid.ColumnDefinitions.Clear();
                ButtonGrid.RowDefinitions.Clear();
                for (var i = 0; i < BoardColumns; i++)
                    ButtonGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
                for (var i = 0; i < BoardRows; i++)
                    ButtonGrid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star));

                //Add empty buttons to grid where there are none
                //Fallback for when the board is loaded from a file and not all buttons are present
                //Rows first then columns
                for (var i = 0; i < BoardRows; i++)
                {
                    for (var j = 0; j < BoardColumns; j++)
                    {
                        if (!Sounds.All(x => x.Column != j || x.Row != i))
                            continue;
                        var button = new SoundModel(this)
                        {
                            Column = j,
                            Row = i
                        };
                        Sounds.Add(button);
                    }
                }
            };
            return ButtonGrid;
        });
    }

    partial void OnBoardColumnsChanged(int value)
    {
        //Ensure the value is at least 1
        if (value < 1)
            BoardColumns = 1;

        //if new value is smaller than the current value, remove columns, make sure to remove buttons in that column before
        //if new value is bigger than the current value, add columns and add buttons to the grid
        var oldValue = ButtonGrid.ColumnDefinitions.Count;
        if (oldValue > value)
            for (var i = oldValue - 1; i >= value; i--)
            {
                foreach (var sound in Sounds.Where(x => x.Column == i).ToList())
                    Sounds.Remove(sound);
                ButtonGrid.ColumnDefinitions.RemoveAt(i);
            }
        else
            for (var i = oldValue; i < value; i++)
            {
                ButtonGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
                for (var j = 0; j < BoardRows; j++)
                {
                    if (!Sounds.All(x => x.Column != i || x.Row != j))
                        continue;
                    var button = new SoundModel(this)
                    {
                        Column = i,
                        Row = j
                    };
                    Sounds.Add(button);
                }
            }
        
        foreach (var sound in Sounds)
            sound.UpdateIndex();
    }

    partial void OnBoardRowsChanged(int value)
    {
        //Ensure the value is at least 1
        if (value < 1)
            BoardRows = 1;

        //if new value is smaller than the current value, remove rows, make sure to remove buttons in that row before
        //if new value is bigger than the current value, add rows and add buttons to the grid
        var oldValue = ButtonGrid.RowDefinitions.Count;
        if (oldValue > value)
            for (var i = oldValue - 1; i >= value; i--)
            {
                foreach (var sound in Sounds.Where(x => x.Row == i).ToList())
                    Sounds.Remove(sound);
                ButtonGrid.RowDefinitions.RemoveAt(i);
            }
        else
            for (var i = oldValue; i < value; i++)
            {
                ButtonGrid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star));
                for (var j = 0; j < BoardColumns; j++)
                {
                    if (!Sounds.All(x => x.Column != j || x.Row != i))
                        continue;
                    var button = new SoundModel(this)
                    {
                        Column = j,
                        Row = i
                    };
                    Sounds.Add(button);
                }
            }
        
        foreach (var sound in Sounds)
            sound.UpdateIndex();
    }


    [RelayCommand]
    private void Edit()
    {
        IsEditing = !IsEditing;
    }

    public void Remove()
    {
        if (Parent.SelectedSoundBoard == this)
            Parent.SelectedSoundBoard = null;
        Parent.SoundBoards.Remove(this);
    }

    public SoundBoard ToEntity()
    {
        return new SoundBoard()
        {
            Name = Name,
            Columns = BoardColumns,
            Rows = BoardRows,
            Sounds = Sounds.Select(x => x.ToEntity()).ToList()
        };
    }
}