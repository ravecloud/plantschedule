import sys
import json
import pandas as pd
import matplotlib.pyplot as plt
import matplotlib.dates as mdates
from datetime import datetime, timedelta
from PyQt5 import QtWidgets, QtCore
from matplotlib.backends.backend_qt5agg import FigureCanvasQTAgg as FigureCanvas

# --- Gantt Chart Canvas using Matplotlib ---
class GanttChartCanvas(FigureCanvas):
    def __init__(self, parent=None):
        self.fig, self.ax = plt.subplots(figsize=(10, 6))
        super().__init__(self.fig)
        self.setParent(parent)

    def plot_gantt(self, resources, time_range, sim_start):
        # Clear any previous plot
        self.ax.clear()
        # Set the x-axis limits to the selected time range
        self.ax.set_xlim(time_range[0], time_range[1])
        
        # Prepare y-axis labels: one horizontal lane per resource
        y_labels = []
        current_y = 0
        for resource in resources:
            res_name = resource.get("Name", "Unknown")
            y_labels.append(res_name)
            current_y += 1

        #if y_labels is not None:
        #   y_labels = list(reversed(y_labels))

        # Define colors by operation type (adjust or extend as needed)
        color_map = {
            "Process": "green",
            "Changeover": "orange",
            "Wait": "gray",
            "Idle": "lightblue"
        }

        # Loop over resources and plot each operation as a horizontal bar.
        for i, resource in enumerate(resources):
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

                # Skip operations that fall completely outside the user–defined time range.
                if end_time < time_range[0] or start_time > time_range[1]:
                    continue

                # Determine color based on the operation’s name.
                op_name = op.get("Name", "")
                color = "blue"  # default color
                for key, c in color_map.items():
                    if key.lower() in op_name.lower():
                        color = c
                        break

                # Compute left coordinate and bar width (convert duration to hours)
                left = mdates.date2num(start_time)
                width = (end_time - start_time).total_seconds() / (24*3600.0)

                # Draw the horizontal bar on lane i
                self.ax.barh(i, width, left=left, height=0.8, color=color, edgecolor="black")
                # Optionally annotate the bar with the Order (job) ID
                order = op.get("Order", "")
                self.ax.text(left + width/2, i, order, ha="center", va="center", color="white", fontsize=8)

        # Format the x-axis to show date–time labels
        self.ax.xaxis_date()
        self.fig.autofmt_xdate()
        # Draw a vertical dashed red line for the simulation start (if available)
        if sim_start:
            try:
                sim_start_dt = datetime.fromisoformat(sim_start)
                self.ax.axvline(x=mdates.date2num(sim_start_dt), color="red", linestyle="--", label="Sim Start")
            except Exception as e:
                print("Sim Start time parse error:", e)
        self.ax.set_yticks(range(len(y_labels)))
        self.ax.set_yticklabels(y_labels)
        self.ax.set_title("Gantt Chart")
        self.ax.legend()
        self.draw()

# --- Main Application Window ---
class MainWindow(QtWidgets.QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("Gantt Chart Viewer")
        self.resize(1200, 800)
        self.df = None  # DataFrame for the CSV data
        self.current_row = None

        # Create the central widget and layout
        centralWidget = QtWidgets.QWidget()
        self.setCentralWidget(centralWidget)
        mainLayout = QtWidgets.QVBoxLayout(centralWidget)

        # --- Top Control Panel ---
        controlLayout = QtWidgets.QHBoxLayout()
        mainLayout.addLayout(controlLayout)

        # Button to load CSV file
        self.openButton = QtWidgets.QPushButton("Open CSV")
        self.openButton.clicked.connect(self.open_csv)
        controlLayout.addWidget(self.openButton)

        # Slider for selecting a row (simulation entry)
        self.rowSlider = QtWidgets.QSlider(QtCore.Qt.Horizontal)
        self.rowSlider.setMinimum(0)
        self.rowSlider.valueChanged.connect(self.slider_changed)
        controlLayout.addWidget(self.rowSlider)

        # Label for displaying row metadata
        self.metadataLabel = QtWidgets.QLabel("Metadata: ")
        controlLayout.addWidget(self.metadataLabel)

        # Time range selectors (for fixed chart start/end)
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

        # Button to export the chart as an image or PDF
        self.exportButton = QtWidgets.QPushButton("Export Chart")
        self.exportButton.clicked.connect(self.export_chart)
        controlLayout.addWidget(self.exportButton)

        # --- Matplotlib Canvas for the Gantt Chart ---
        self.canvas = GanttChartCanvas(self)
        mainLayout.addWidget(self.canvas)

    def open_csv(self):
        options = QtWidgets.QFileDialog.Options()
        filename, _ = QtWidgets.QFileDialog.getOpenFileName(self, "Open CSV File", "", 
                                                            "CSV Files (*.csv);;All Files (*)", options=options)
        if filename:
            # Read the CSV using semicolon as delimiter
            self.df = pd.read_csv(filename, delimiter=";")
            if not self.df.empty:
                self.rowSlider.setMaximum(len(self.df) - 1)
                self.rowSlider.setValue(0)  # start at the first row
                print(self.df.columns)
                print(self.df.iloc[0]['Sim Start'])
                # Assuming your string format is "M/d/yyyy h:mm:ss AP"
                self.update_view()

    def slider_changed(self, value):
        self.update_view()

    def update_view(self):
        if self.df is None:
            return
        row_index = self.rowSlider.value()
        self.current_row = self.df.iloc[row_index]
        # Update metadata display using the required columns.
        meta_keys = ["Generation", "Individual", "Fitness", "Age", "Measure", "Mean", "Std"]
        meta_text = " | ".join(f"{key}: {self.current_row.get(key, '')}" for key in meta_keys)
        self.metadataLabel.setText(meta_text)

        # Extract the simulation start time from the CSV (assumed column "Sim Start")
        sim_start = self.current_row.get("Sim Start", None)
        
        # Update the QDateTimeEdit widgets
        sim_start_str = self.df.iloc[0]["Sim Start"]
        dt_obj = datetime.strptime(sim_start_str, "%m/%d/%Y %I:%M:%S %p") - timedelta(hours=1)
        sim_start_qdatetime = QtCore.QDateTime(dt_obj.year, dt_obj.month, dt_obj.day, dt_obj.hour, dt_obj.minute, dt_obj.second)
        self.startTimeEdit.setDateTime(sim_start_qdatetime)
        
        sim_end_str = self.df.iloc[0][" Sim End"]
        dt_obj = datetime.strptime(sim_start_str, "%m/%d/%Y %I:%M:%S %p") - timedelta(hours=-32)
        sim_end_qdatetime = QtCore.QDateTime(dt_obj.year, dt_obj.month, dt_obj.day, dt_obj.hour, dt_obj.minute, dt_obj.second)
        self.endTimeEdit.setDateTime(sim_end_qdatetime)

        json_data_str = self.current_row.iloc[-1]
        try:
            schedule_data = json.loads(json_data_str)
        except Exception as e:
            print("Error parsing JSON:", e)
            schedule_data = {}

        resources = schedule_data.get("Resources", [])
        # Get the time range from the QDateTimeEdit controls.
        start_dt = self.startTimeEdit.dateTime().toPyDateTime()
        end_dt = self.endTimeEdit.dateTime().toPyDateTime()
        time_range = (start_dt, end_dt)

        # Update (redraw) the Gantt chart
        self.canvas.plot_gantt(resources, time_range, sim_start)

    def export_chart(self):
        # Export the current chart as an image file.
        options = QtWidgets.QFileDialog.Options()
        filename, _ = QtWidgets.QFileDialog.getSaveFileName(self, "Export Chart", "", 
                                                            "PNG Files (*.png);;PDF Files (*.pdf)", options=options)
        if filename:
            self.canvas.fig.savefig(filename)
            QtWidgets.QMessageBox.information(self, "Export", f"Chart exported to {filename}")

# --- Main Program Entry ---
if __name__ == "__main__":
    app = QtWidgets.QApplication(sys.argv)
    mainWin = MainWindow()
    mainWin.show()
    sys.exit(app.exec_())
