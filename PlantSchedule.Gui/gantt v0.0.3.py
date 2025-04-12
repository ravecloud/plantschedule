import sys
import json
import pandas as pd
import matplotlib.pyplot as plt
import matplotlib.dates as mdates
from datetime import datetime, timedelta
import pyqtgraph as pg
from PyQt5 import QtWidgets, QtCore
from matplotlib.backends.backend_qt5agg import FigureCanvasQTAgg as FigureCanvas

# --- Gantt Chart Canvas using PyQtGraph ---
class GanttChartCanvas(pg.PlotWidget):
    def __init__(self, parent=None, tooltip_label=None):
        super().__init__(parent)
        self.tooltip_label = tooltip_label  # Reference to external label for tooltip display
        self.operation_bars = []
        self.setBackground("w")
        self.showGrid(x=True, y=True)
        self.getAxis("bottom").setLabel("Time")
        self.getAxis("left").setLabel("Resources")
        self.setMouseTracking(True)
        self.scene().sigMouseMoved.connect(self.on_motion)

    def plot_gantt(self, resources, time_range, sim_start_str):
        self.clear()
        self.operation_bars = []
        y_labels = []
        color_map = {
            "Process": "b",
            "Changeover": "gray",
            "Wait": "w",
            "Idle": "w"
        }
        for i, resource in enumerate(resources):
            res_name = resource.get("Name", "Unknown")
            y_labels.append(res_name)
            ops = resource.get("Operations", [])
            for op in ops:
                start_str = op.get("Start")
                end_str = op.get("End")
                if not start_str or not end_str:
                    continue
                try:
                    start_time = datetime.fromisoformat(start_str)
                    end_time = datetime.fromisoformat(end_str)
                except Exception as e:
                    print("Time parsing error:", e)
                    continue
                if end_time < time_range[0] or start_time > time_range[1]:
                    continue
                op_name = op.get("Name", "")
                op_order = op.get("Order", "")
                color = "b"
                for key, c in color_map.items():
                    if key.lower() in op_name.lower():
                        color = c
                        if key == "Process" and "Rush" in op_order:
                            color = "r"
                        if key == "Changeover" and "Rush" in op_order:
                            color = "orange"
                        break
                x_start = start_time.timestamp()
                x_end = end_time.timestamp()
                self.plot([x_start, x_end], [i, i], pen=pg.mkPen(color, width=8))
                self.operation_bars.append((x_start, x_end, i, op))
        if sim_start_str:
            try:
                sim_start_dt = datetime.strptime(sim_start_str, "%m/%d/%Y %I:%M:%S %p")
                self.addItem(pg.InfiniteLine(pos=sim_start_dt.timestamp(), angle=90, pen=pg.mkPen("r", width=2, style=QtCore.Qt.DashLine)))
            except Exception as e:
                print("Sim Start time parse error:", e)
    
    def on_motion(self, pos):
        mouse_point = self.plotItem.vb.mapSceneToView(pos)
        x, y = mouse_point.x(), mouse_point.y()
        for x_start, x_end, bar_y, op in self.operation_bars:
            if x_start <= x <= x_end and abs(y - bar_y) < 0.5:
                tooltip_text = "\n".join(f"{key}: {value}" for key, value in op.items())
                if self.tooltip_label:
                    self.tooltip_label.setText(tooltip_text)
                return
        if self.tooltip_label:
            self.tooltip_label.setText("")

# --- Main Application Window ---
class MainWindow(QtWidgets.QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("Gantt Chart Viewer")
        self.resize(1200, 800)
        self.df = None  # DataFrame for the CSV data
        self.current_row = None
        self.fixed_range_set = False  # Flag to set fixed start/end only once

        # Create the central widget and layout.
        centralWidget = QtWidgets.QWidget()
        self.setCentralWidget(centralWidget)
        mainLayout = QtWidgets.QVBoxLayout(centralWidget)

        # --- Top Control Panel ---
        controlLayout = QtWidgets.QHBoxLayout()
        mainLayout.addLayout(controlLayout)

        # Button to load CSV file.
        self.openButton = QtWidgets.QPushButton("Open CSV")
        self.openButton.clicked.connect(self.open_csv)
        controlLayout.addWidget(self.openButton)

        # Slider for selecting a row (simulation entry).
        self.rowSlider = QtWidgets.QSlider(QtCore.Qt.Horizontal)
        self.rowSlider.setMinimum(0)
        self.rowSlider.valueChanged.connect(self.slider_changed)
        controlLayout.addWidget(self.rowSlider)

        # Label for displaying row metadata.
        self.metadataLabel = QtWidgets.QLabel("Metadata: ")
        controlLayout.addWidget(self.metadataLabel)

        # Fixed time range selectors (set from CSV’s first row).
        self.startTimeEdit = QtWidgets.QDateTimeEdit(QtCore.QDateTime.currentDateTime())
        self.startTimeEdit.setDisplayFormat("yyyy-MM-dd HH:mm:ss")
        self.startTimeEdit.setCalendarPopup(True)
        controlLayout.addWidget(QtWidgets.QLabel("Chart Start:"))
        controlLayout.addWidget(self.startTimeEdit)

        self.endTimeEdit = QtWidgets.QDateTimeEdit(QtCore.QDateTime.currentDateTime().addDays(1))
        self.endTimeEdit.setDisplayFormat("yyyy-MM-dd HH:mm:ss")
        self.endTimeEdit.setCalendarPopup(True)
        controlLayout.addWidget(QtWidgets.QLabel("Chart End:"))
        controlLayout.addWidget(self.endTimeEdit)

        # Play/Pause button for automatic slider movement.
        self.playButton = QtWidgets.QPushButton("Play")
        self.playButton.setCheckable(True)
        self.playButton.clicked.connect(self.toggle_play)
        controlLayout.addWidget(self.playButton)

        # A spin box to set the delay (in seconds) between slider moves.
        self.delaySpinBox = QtWidgets.QSpinBox()
        self.delaySpinBox.setMinimum(1)
        self.delaySpinBox.setMaximum(60)
        self.delaySpinBox.setValue(1)  # default 1 second delay
        controlLayout.addWidget(QtWidgets.QLabel("Delay (ms):"))
        controlLayout.addWidget(self.delaySpinBox)

        # Button to export the chart as an image or PDF.
        self.exportButton = QtWidgets.QPushButton("Export Chart")
        self.exportButton.clicked.connect(self.export_chart)
        controlLayout.addWidget(self.exportButton)

        # --- Matplotlib Canvas for the Gantt Chart ---
        leftLayout = QtWidgets.QVBoxLayout()
        mainLayout.addLayout(leftLayout)
        controlLayout = QtWidgets.QHBoxLayout()
        leftLayout.addLayout(controlLayout)
        self.tooltip_label = QtWidgets.QLabel("Hover over a bar to see details")
        mainLayout.addWidget(self.tooltip_label)
        self.canvas = GanttChartCanvas(self, self.tooltip_label)
        leftLayout.addWidget(self.canvas)
        #self.canvas = GanttChartCanvas(self)
        #mainLayout.addWidget(self.canvas)

        # Timer for play mode.
        self.play_timer = QtCore.QTimer(self)
        self.play_timer.timeout.connect(self.advance_slider)

    def open_csv(self):
        options = QtWidgets.QFileDialog.Options()
        filename, _ = QtWidgets.QFileDialog.getOpenFileName(self, "Open CSV File", "",
                                                            "CSV Files (*.csv);;All Files (*)",
                                                            options=options)
        if filename:
            # Read the CSV using semicolon as delimiter.
            self.df = pd.read_csv(filename, delimiter=";")
            if not self.df.empty:
                self.rowSlider.setMaximum(len(self.df) - 1)
                self.rowSlider.setValue(0)  # start at the first row
                print("CSV Columns:", self.df.columns)
                print("First row Sim Start:", self.df.iloc[0]['Sim Start'])
                # Set the fixed start/end time range from row 0.
                self.set_fixed_time_range()
                # Update the view for the current row.
                self.update_view()

    def set_fixed_time_range(self):
        """Set the chart’s fixed start/end times using the first row of the CSV."""
        if self.df is None or self.fixed_range_set:
            return

        try:
            fixed_start_str = self.df.iloc[0]["Sim Start"]
            dt_obj = datetime.strptime(fixed_start_str, "%m/%d/%Y %I:%M:%S %p")
            fixed_start_qdatetime = QtCore.QDateTime(dt_obj.year, dt_obj.month, dt_obj.day,
                                                     dt_obj.hour, dt_obj.minute, dt_obj.second)
            self.startTimeEdit.setDateTime(fixed_start_qdatetime)
        except Exception as e:
            print("Error parsing fixed start:", e)

        try:
            # Adjust key name if needed (note: leading/trailing spaces may occur).
            fixed_end_str = self.df.iloc[0]["Sim Start"].strip()  # remove extra spaces
            dt_obj = datetime.strptime(fixed_end_str, "%m/%d/%Y %I:%M:%S %p") - timedelta(hours=-32)
            fixed_end_qdatetime = QtCore.QDateTime(dt_obj.year, dt_obj.month, dt_obj.day,
                                                   dt_obj.hour, dt_obj.minute, dt_obj.second)
            self.endTimeEdit.setDateTime(fixed_end_qdatetime)
        except Exception as e:
            print("Error parsing fixed end:", e)

        self.fixed_range_set = True

    def slider_changed(self, value):
        self.update_view()

    def update_view(self):
        if self.df is None:
            return

        # Use the slider value to pick the current row.
        row_index = self.rowSlider.value()
        self.current_row = self.df.iloc[row_index]
        # Update metadata display (adjust keys as needed).
        meta_keys = ["Generation", "Individual", "Fitness", "Age", "Measure", "Mean", "Std"]
        meta_text = " | ".join(f"{key}: {self.current_row.get(key, '')}" for key in meta_keys)
        self.metadataLabel.setText(meta_text)

        # Get the fixed time range from the QDateTimeEdit controls.
        start_dt = self.startTimeEdit.dateTime().toPyDateTime()
        end_dt = self.endTimeEdit.dateTime().toPyDateTime()
        time_range = (start_dt, end_dt)

        # Use the current row’s simulation start (for the moving vertical line).
        sim_start = self.current_row.get("Sim Start", None)

        # Get the schedule JSON data from the last column.
        json_data_str = self.current_row.iloc[-1]
        try:
            schedule_data = json.loads(json_data_str)
        except Exception as e:
            print("Error parsing JSON:", e)
            schedule_data = {}

        resources = schedule_data.get("Resources", [])
        # Update (redraw) the Gantt chart.
        self.canvas.plot_gantt(resources, time_range, sim_start)

    def toggle_play(self, checked):
        """Start or stop the automatic slider movement."""
        if checked:
            self.playButton.setText("Pause")
            delay_ms = self.delaySpinBox.value() * 1
            self.play_timer.start(delay_ms)
        else:
            self.playButton.setText("Play")
            self.play_timer.stop()

    def advance_slider(self):
        """Advance the slider by one step. Reset or stop when reaching the end."""
        new_value = self.rowSlider.value() + 1
        if new_value > self.rowSlider.maximum():
            # Option 1: Loop back to 0.
            new_value = 0
            # Option 2: Stop the play mode:
            # self.playButton.setChecked(False)
            # self.playButton.setText("Play")
            # self.play_timer.stop()
        self.rowSlider.setValue(new_value)

    def export_chart(self):
        # Export the current chart as an image file.
        options = QtWidgets.QFileDialog.Options()
        filename, _ = QtWidgets.QFileDialog.getSaveFileName(self, "Export Chart", "",
                                                            "PNG Files (*.png);;PDF Files (*.pdf)",
                                                            options=options)
        if filename:
            self.canvas.fig.savefig(filename)
            QtWidgets.QMessageBox.information(self, "Export", f"Chart exported to {filename}")


# --- Main Program Entry ---
if __name__ == "__main__":
    app = QtWidgets.QApplication(sys.argv)
    mainWin = MainWindow()
    mainWin.show()
    sys.exit(app.exec_())
