# ODB++ Extractor User Guide

This application is designed for parsing ODB++ designs and export the component data into structured XML.

<div align="center">
  <img src="images/main_interface.png" height="400" alt="Main Interface" />
</div>

## 1. Supported Input

The application is strictly read-only and will not modify your original design files.

* **Archives:** For `.tgz`, `.tar.gz`, `.zip`, and `.tar` files, click **Browse file**.
* **Directories:** For extracted ODB++ folders, click **Browse folder**.

## 2. Quick Extraction Workflow

1. Open the application and load your ODB++ file or folder.
2. Select the target **Step** and component **Layer** from the dropdown menus.
3. Configure your export options (Origin, Unit, Axis flips) to map correctly to your target system.
4. Click **Preview** to visually verify the layout and coordinate alignment.
5. Export the data:
   * **Export Current Layer:** Generates a single XML file for the selected layer.
   * **Export All Layers:** Generates individual XML files for every component layer in the step.

<div class="page"/>

## 3. Coordinate & Export Parameters

Adjust these settings to ensure the exported data aligns precisely with your system's coordinate requirements. Changes reflect instantly in both the visual preview and the data grid.

* **Origin:**
  * **Top-left:** Y-axis values increase as you move downward.
  * **Bottom-left:** Y-axis values increase as you move upward.
* **Unit:** Choose **MM** or **INCH**.
* **Axis Flipping:**
  * **Flip X-axis:** Mirrors all X coordinates across the PCB width.
  * **Flip Y-axis:** Mirrors all Y coordinates across the PCB height.

## 4. Export Result

Files are saved to your designated directory using the specific naming convention: `<job>_<timestamp>_components_<layer>.xml`.

<div class="page"/>

## 5. Visual Validation (Viewer)

The built-in viewer allows you to inspect components and overlay background images (such as a reference photo) for visual alignment checks before exporting.

<div align="center">
  <img src="images/viewer.png" height="400" alt="Viewer" />
</div>

**Basic Controls:**

* **Zoom:** Mouse wheel
* **Pan:** Left-click and drag
* **Inspect:** Click any component to view its Package, Center X/Y, Width, Length, and Rotation.
* **Search:** Use the side panel to filter components by name or package type.
* **Context Menu:** Right-click the canvas to reset view, or load, clear, lock, or reset a background image.

**Background Alignment Shortcuts (when unlocked):**

* **Move Image:** `Ctrl` + `Left Drag`
* **Scale Image:** `Ctrl` + `Mouse Wheel`
* **Fine Zoom:** `Ctrl` + `+` or `-`
* **Extra-Fine Zoom:** `Ctrl` + `Shift` + `+` or `-`

## 6. Notes

* Always verify that your target system's coordinate convention matches your Origin, Unit, and Axis flip settings before export.
* You can change the application language at any time by pressing `Ctrl + Shift + L`.
