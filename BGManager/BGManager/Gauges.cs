using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace BGManager;

public class Gauges
{
	public enum DGVConfigType
	{
		setEditableColumns,
		setInvisibleColumns,
		setSortableColumns
	}

	public static class EnumUtil
	{
		public static IEnumerable<T> GetValues<T>()
		{
			return Enum.GetValues(typeof(T)).Cast<T>();
		}
	}

	private static StreamWriter _logFile = null;

	private static string _logFilePath = "";

	private static uint _logLines = 0u;

	public static StreamWriter LogFile => _logFile;

	public static void removePrefix(string prefix, ref string s)
	{
		if (s.IndexOf(prefix) == 0)
		{
			s = s.Substring(prefix.Length);
		}
	}

	public static void removedPrefixWord(string prefix, string divider, ref string s)
	{
		if (s.IndexOf(prefix) == 0 && s.IndexOf(divider) > 0)
		{
			s = s.Substring(s.IndexOf(divider) + 1);
		}
	}

	public static void configureDataGridView(ref DataGridView dgv, bool autoGenerateColumns, bool readOnly)
	{
		dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
		dgv.MultiSelect = false;
		dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
		dgv.AllowUserToResizeRows = false;
		dgv.RowHeadersVisible = false;
		dgv.AllowUserToAddRows = !readOnly;
		dgv.ReadOnly = readOnly;
		dgv.ClearSelection();
		dgv.Columns.Clear();
		dgv.AutoGenerateColumns = autoGenerateColumns;
	}

	public static void configureDataGridView(ref DataGridView dgv, DGVConfigType task, List<string> columns)
	{
		foreach (DataGridViewColumn c in dgv.Columns)
		{
			switch (task)
			{
			case DGVConfigType.setEditableColumns:
				if (columns.Contains(c.Name))
				{
					c.ReadOnly = false;
					c.DefaultCellStyle.BackColor = Color.LightYellow;
				}
				else
				{
					c.ReadOnly = true;
				}
				break;
			case DGVConfigType.setInvisibleColumns:
				c.Visible = !columns.Contains(c.Name);
				break;
			case DGVConfigType.setSortableColumns:
				c.SortMode = (columns.Contains(c.Name) ? DataGridViewColumnSortMode.Automatic : DataGridViewColumnSortMode.NotSortable);
				break;
			}
		}
	}

	public static void onDGVClickColumnHeader(ref DataGridView dgv, int columnIndex, PropertyDescriptor oldSortedColumn, ListSortDirection oldLSD)
	{
		DataGridViewColumn newColumn = ((columnIndex >= 0 && columnIndex < dgv.Columns.Count) ? dgv.Columns[columnIndex] : null);
		DataGridViewColumn oldColumn = ((oldSortedColumn == null) ? null : dgv.Columns[oldSortedColumn.Name]);
		ListSortDirection direction;
		if (oldColumn != null)
		{
			if (oldColumn == newColumn && oldLSD == ListSortDirection.Ascending)
			{
				direction = ListSortDirection.Descending;
			}
			else
			{
				if (newColumn.SortMode == DataGridViewColumnSortMode.NotSortable)
				{
					return;
				}
				direction = ListSortDirection.Ascending;
				oldColumn.HeaderCell.SortGlyphDirection = SortOrder.None;
			}
		}
		else
		{
			direction = ListSortDirection.Ascending;
		}
		if (newColumn == null)
		{
			MessageBox.Show("Select a single column and try again.", "Error: Invalid Selection", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
		else if (newColumn.SortMode != 0)
		{
			newColumn.HeaderCell.SortGlyphDirection = ((direction == ListSortDirection.Ascending) ? SortOrder.Ascending : SortOrder.Descending);
			dgv.Sort(newColumn, direction);
			dgv.Invalidate();
		}
	}

	internal static DataGridViewColumn _CreateColumn(string name, float fillWeight, DataGridViewColumnSortMode sortMode, bool readOnly, bool visible, string dataPropertyName, HashSet<string> variants, string toolTip = "")
	{
		DataGridViewColumn column = new DataGridViewColumn();
		column.Name = name;
		DataGridViewCell cell = null;
		if (variants == null)
		{
			cell = new DataGridViewTextBoxCell();
		}
		else
		{
			DataGridViewComboBoxCell bcell = new DataGridViewComboBoxCell();
			foreach (string s in variants)
			{
				bcell.Items.Add(s);
			}
			bcell.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
			cell = bcell;
		}
		column.CellTemplate = cell;
		column.FillWeight = fillWeight;
		column.SortMode = sortMode;
		column.ReadOnly = readOnly;
		column.ToolTipText = toolTip;
		if (!readOnly)
		{
			column.DefaultCellStyle.BackColor = Color.LightYellow;
		}
		column.Visible = visible;
		column.DataPropertyName = dataPropertyName;
		return column;
	}

	public static DataGridViewColumn CreateColumn(string name, float fillWeight, DataGridViewColumnSortMode sortMode, bool readOnly, bool visible, string dataPropertyName, HashSet<string> variants, string toolTip = "")
	{
		return _CreateColumn(name, fillWeight, sortMode, readOnly, visible, dataPropertyName, variants, toolTip);
	}

	public static DataGridViewColumn CreateColumn(string name, float fillWeight, DataGridViewColumnSortMode sortMode, bool readOnly, bool visible, string dataPropertyName, string toolTip = "")
	{
		return _CreateColumn(name, fillWeight, sortMode, readOnly, visible, dataPropertyName, null, toolTip);
	}

	public static DataGridViewColumn CreateCheckBoxColumn(string name, float fillWeight, DataGridViewColumnSortMode sortMode, bool readOnly, bool visible, string dataPropertyName, string toolTip = "")
	{
		DataGridViewColumn column = new DataGridViewColumn();
		column.Name = name;
		DataGridViewCell cell = new DataGridViewCheckBoxCell();
		column.CellTemplate = cell;
		column.FillWeight = fillWeight;
		column.SortMode = sortMode;
		column.ReadOnly = readOnly;
		column.ToolTipText = toolTip;
		if (!readOnly)
		{
			column.DefaultCellStyle.BackColor = Color.LightCoral;
		}
		column.Visible = visible;
		column.DataPropertyName = dataPropertyName;
		return column;
	}

	public static DataGridViewColumn CreateButtonColumn(string name, float fillWeight, DataGridViewColumnSortMode sortMode, bool readOnly, bool visible, string dataPropertyName, string toolTip = "")
	{
		DataGridViewColumn column = new DataGridViewColumn();
		column.Name = name;
		DataGridViewButtonCell cell = new DataGridViewButtonCell();
		cell.FlatStyle = FlatStyle.Popup;
		cell.UseColumnTextForButtonValue = false;
		column.CellTemplate = cell;
		column.FillWeight = fillWeight;
		column.SortMode = sortMode;
		column.ReadOnly = readOnly;
		column.ToolTipText = toolTip;
		if (!readOnly)
		{
			column.DefaultCellStyle.BackColor = Color.LightYellow;
		}
		column.Visible = visible;
		column.DataPropertyName = dataPropertyName;
		return column;
	}

	public static int NumericCompare(int a, int b)
	{
		return (a > b) ? 1 : ((a < b) ? (-1) : 0);
	}

	public static int NumericCompare(uint a, uint b)
	{
		return (a > b) ? 1 : ((a < b) ? (-1) : 0);
	}

	public static void BuildFileList(ref List<string> fileList, string startDir, string wildCard)
	{
		string[] files = Directory.GetFiles(startDir, wildCard);
		foreach (string file in files)
		{
			fileList.Add(file);
		}
		files = Directory.GetDirectories(startDir);
		foreach (string subDir in files)
		{
			BuildFileList(ref fileList, subDir, wildCard);
		}
	}

	public static void InitLogFile(string path)
	{
		if (_logFile != null)
		{
			_logFile.Close();
		}
		_logFilePath = path;
		_logFile = new StreamWriter(path);
	}

	public static void CloseLogFile(bool keepNonEmpty)
	{
		if (_logFile != null)
		{
			_logFile.Close();
			if (_logLines == 0 || !keepNonEmpty)
			{
				File.Delete(_logFilePath);
			}
		}
	}

	public static void Log(string s)
	{
		if (LogFile != null)
		{
			LogFile.WriteLine($"{DateTime.Now:MM/dd H:mm:ss} {s}");
			_logLines++;
		}
	}

	public static void Log(string format, params object[] args)
	{
		if (LogFile != null)
		{
			string fmt = ((args == null) ? format : string.Format(format, args));
			Log(fmt);
		}
	}
}
