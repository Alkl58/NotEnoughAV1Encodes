using System.IO;
using System.Xml;

namespace NotEnoughAV1Encodes
{
    class SaveSettings : MainWindow
    {
        public SaveSettings(bool SaveProfile, string SaveName)
        {
            string directory = "";
            if (SaveProfile)
            {
                // Path to Profile Save
                directory = Path.Combine(Directory.GetCurrentDirectory(), "Profiles", SaveName + ".xml");
                // Check Creates Profile Folder
                if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Profiles")))
                    Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Profiles"));
            }

            // New XmlWriter instance
            XmlWriter writer = XmlWriter.Create(directory);
            // Write Start Element
            writer.WriteStartElement("Settings");
            
            // ═══════════════════════════════════ Splitting ═══════════════════════════════════
            writer.WriteElementString("SplittingMethod",                    ComboBoxSplittingMethod.SelectedIndex.ToString());          // Splitting Method
            if (ComboBoxSplittingMethod.SelectedIndex == 0)
            {
                // FFmpeg Scene Detect
                writer.WriteElementString("SplittingThreshold",             TextBoxSplittingThreshold.Text);                            // Splitting Threshold
            }
            else if (ComboBoxSplittingMethod.SelectedIndex == 2)
            {
                // Chunking Method
                if (CheckBoxSplittingReencode.IsChecked == true)
                {
                    writer.WriteElementString("SplittingReencode",          ComboBoxSplittingReencodeMethod.SelectedIndex.ToString());  // Splitting Reencode Codec
                }
                writer.WriteElementString("SplittingReencodeActive",        CheckBoxSplittingReencode.IsChecked.ToString());            // Splitting Reencode Active
                writer.WriteElementString("SplittingReencodeLength",        TextBoxSplittingChunkLength.Text);                          // Splitting Chunk Length
            }

            // ════════════════════════════════════ Filters ════════════════════════════════════

            writer.WriteElementString("FilterCrop",                 CheckBoxFiltersCrop.IsChecked.ToString());                          // Filter Crop (Boolean)
            if (CheckBoxFiltersCrop.IsChecked == true)
            {
                // Cropping
                writer.WriteElementString("FilterCropTop",          TextBoxFiltersCropTop.Text);                                        // Filter Crop Top
                writer.WriteElementString("FilterCropBottom",       TextBoxFiltersCropBottom.Text);                                     // Filter Crop Bottom
                writer.WriteElementString("FilterCropLeft",         TextBoxFiltersCropLeft.Text);                                       // Filter Crop Left
                writer.WriteElementString("FilterCropRight",        TextBoxFiltersCropRight.Text);                                      // Filter Crop Right
            }

            writer.WriteElementString("FilterResize",               CheckBoxFiltersResize.IsChecked.ToString());                        // Filter Resize (Boolean)
            if (CheckBoxFiltersResize.IsChecked == true)
            {
                // Resize
                writer.WriteElementString("FilterResizeWidth",      TextBoxFiltersResizeWidth.Text);                                    // Filter Resize Width
                writer.WriteElementString("FilterResizeHeight",     TextBoxFiltersResizeHeight.Text);                                   // Filter Resize Height
                writer.WriteElementString("FilterResizeAlgo",       ComboBoxFiltersScaling.SelectedIndex.ToString());                   // Filter Resize Scaling Algorithm
            }

            writer.WriteElementString("FilterRotate",               CheckBoxFiltersRotate.IsChecked.ToString());                        // Filter Rotate (Boolean)
            if (CheckBoxFiltersRotate.IsChecked == true)
            {
                // Rotating
                writer.WriteElementString("FilterRotateAmount",     ComboBoxFiltersRotate.SelectedIndex.ToString());                    // Filter Rotate
            }

            writer.WriteElementString("FilterDeinterlace",          CheckBoxFiltersDeinterlace.IsChecked.ToString());                   // Filter Deinterlace (Boolean)
            if (CheckBoxFiltersDeinterlace.IsChecked == true)
            {
                // Deinterlacing
                writer.WriteElementString("FilterDeinterlaceType",  ComboBoxFiltersDeinterlace.SelectedIndex.ToString());               // Filter Deinterlace
            }

            // ═════════════════════════════ Basic Video Settings ══════════════════════════════
            

            writer.WriteElementString("VideoEncoder",           ComboBoxVideoEncoder.SelectedIndex.ToString());                         // Video Encoder
            writer.WriteElementString("VideoBitDepth",          ComboBoxVideoBitDepth.SelectedIndex.ToString());                        // Video BitDepth
            writer.WriteElementString("VideoSpeed",             SliderVideoSpeed.Value.ToString());                                     // Video Speed
            writer.WriteElementString("VideoPasses",            ComboBoxVideoPasses.SelectedIndex.ToString());                          // Video Passes
            if (RadioButtonVideoConstantQuality.IsChecked == true)
                writer.WriteElementString("VideoQuality",       SliderVideoQuality.Value.ToString());                                   // Video Quality
            if (RadioButtonVideoBitrate.IsChecked == true)
                writer.WriteElementString("VideoBitrate",       TextBoxVideoBitrate.Text);                                              // Video Bitrate

            // ═══════════════════════════ Advanced Video Settings ═════════════════════════════

            writer.WriteElementString("VideoAdvanced",          CheckBoxVideoAdvancedSettings.IsChecked.ToString());                    // Video Advanced Settings
            writer.WriteElementString("VideoAdvancedCustom",    CheckBoxCustomVideoSettings.IsChecked.ToString());                      // Video Advanced Settings Custom

            if (CheckBoxVideoAdvancedSettings.IsChecked == true && CheckBoxCustomVideoSettings.IsChecked == false)
            {
                // Custom Advanced Settings
                if (ComboBoxVideoEncoder.SelectedIndex == 0)
                {
                    // aomenc
                    writer.WriteElementString("VideoAdvancedAomencThreads",     ComboBoxAomencThreads.SelectedIndex.ToString());        // Video Advanced Settings Aomenc Threads
                    writer.WriteElementString("VideoAdvancedAomencTileCols",    ComboBoxAomencTileColumns.SelectedIndex.ToString());    // Video Advanced Settings Aomenc Tile Columns
                    writer.WriteElementString("VideoAdvancedAomencTileRows",    ComboBoxAomencTileRows.SelectedIndex.ToString());       // Video Advanced Settings Aomenc Tile Rows
                    writer.WriteElementString("VideoAdvancedAomencGOP",         TextBoxAomencMaxGOP.Text);                              // Video Advanced Settings Aomenc GOP
                    writer.WriteElementString("VideoAdvancedAomencLag",         TextBoxAomencLagInFrames.Text);                         // Video Advanced Settings Aomenc Lag in Frames
                    writer.WriteElementString("VideoAdvancedAomencSharpness",   ComboBoxAomencSharpness.SelectedIndex.ToString());      // Video Advanced Settings Aomenc Sharpness
                    writer.WriteElementString("VideoAdvancedAomencColorPrim",   ComboBoxAomencColorPrimaries.SelectedIndex.ToString()); // Video Advanced Settings Aomenc Color Primaries
                    writer.WriteElementString("VideoAdvancedAomencColorTrans",  ComboBoxAomencColorTransfer.SelectedIndex.ToString());  // Video Advanced Settings Aomenc Color Transfer
                    writer.WriteElementString("VideoAdvancedAomencColorMatrix", ComboBoxAomencColorMatrix.SelectedIndex.ToString());    // Video Advanced Settings Aomenc Color Matrix
                    writer.WriteElementString("VideoAdvancedAomencColorFormat", ComboBoxAomencColorFormat.SelectedIndex.ToString());    // Video Advanced Settings Aomenc Color Format
                    writer.WriteElementString("VideoAdvancedAomencAQMode",      ComboBoxAomencAQMode.SelectedIndex.ToString());         // Video Advanced Settings Aomenc AQ Mode
                    writer.WriteElementString("VideoAdvancedAomencKFFiltering", ComboBoxAomencKeyFiltering.SelectedIndex.ToString());   // Video Advanced Settings Aomenc Keyframe Filtering
                    writer.WriteElementString("VideoAdvancedAomencTune",        ComboBoxAomencTune.SelectedIndex.ToString());           // Video Advanced Settings Aomenc Tune
                    writer.WriteElementString("VideoAdvancedAomencARNR",        CheckBoxAomencARNRMax.IsChecked.ToString());            // Video Advanced Settings Aomenc ARNR
                    if (CheckBoxAomencARNRMax.IsChecked == true)
                    {
                        writer.WriteElementString("VideoAdvancedAomencARNRMax", ComboBoxAomencARNRMax.SelectedIndex.ToString());        // Video Advanced Settings Aomenc ARNR Max
                        writer.WriteElementString("VideoAdvancedAomencARNRStre",ComboBoxAomencARNRStrength.SelectedIndex.ToString());   // Video Advanced Settings Aomenc ARNR Strength
                    }
                    writer.WriteElementString("VideoAdvancedAomencRowMT",       CheckBoxAomencRowMT.IsChecked.ToString());              // Video Advanced Settings Aomenc Row Mt
                    writer.WriteElementString("VideoAdvancedAomencCDEF",        CheckBoxAomencCDEF.IsChecked.ToString());               // Video Advanced Settings Aomenc CDEF
                }
                else if (ComboBoxVideoEncoder.SelectedIndex == 1)
                {
                    // rav1e
                    writer.WriteElementString("VideoAdvancedRav1eThreads",      ComboBoxRav1eThreads.SelectedIndex.ToString());         // Video Advanced Settings Rav1e Threads
                    writer.WriteElementString("VideoAdvancedRav1eTileCols",     ComboBoxRav1eTileColumns.SelectedIndex.ToString());     // Video Advanced Settings Rav1e Tile Columns
                    writer.WriteElementString("VideoAdvancedRav1eTileRows",     ComboBoxRav1eTileRows.SelectedIndex.ToString());        // Video Advanced Settings Rav1e Tile Rows
                    writer.WriteElementString("VideoAdvancedRav1eGOP",          TextBoxRav1eMaxGOP.Text);                               // Video Advanced Settings Rav1e GOP
                    writer.WriteElementString("VideoAdvancedRav1eRDO",          TextBoxRav1eLookahead.Text);                            // Video Advanced Settings Rav1e RDO Lookahead
                    writer.WriteElementString("VideoAdvancedRav1eColorPrim",    ComboBoxRav1eColorPrimaries.SelectedIndex.ToString());  // Video Advanced Settings Rav1e Color Primaries
                    writer.WriteElementString("VideoAdvancedRav1eColorTrans",   ComboBoxRav1eColorTransfer.SelectedIndex.ToString());   // Video Advanced Settings Rav1e Color Transfer
                    writer.WriteElementString("VideoAdvancedRav1eColorMatrix",  ComboBoxRav1eColorMatrix.SelectedIndex.ToString());     // Video Advanced Settings Rav1e Color Matrix
                    writer.WriteElementString("VideoAdvancedRav1eColorFormat",  ComboBoxRav1eColorFormat.SelectedIndex.ToString());     // Video Advanced Settings Rav1e Color Format
                    writer.WriteElementString("VideoAdvancedRav1eTune",         ComboBoxRav1eTune.SelectedIndex.ToString());            // Video Advanced Settings Rav1e Tune
                    writer.WriteElementString("VideoAdvancedRav1eMastering",    CheckBoxRav1eMasteringDisplay.IsChecked.ToString());    // Video Advanced Settings Rav1e Mastering Display
                    if (CheckBoxRav1eMasteringDisplay.IsChecked == true)
                    {
                        writer.WriteElementString("VideoAdvancedRav1eMasteringGx",  TextBoxRav1eMasteringGx.Text);                      // Video Advanced Settings Rav1e Mastering Display Gx
                        writer.WriteElementString("VideoAdvancedRav1eMasteringGy",  TextBoxRav1eMasteringGy.Text);                      // Video Advanced Settings Rav1e Mastering Display Gy
                        writer.WriteElementString("VideoAdvancedRav1eMasteringBx",  TextBoxRav1eMasteringBx.Text);                      // Video Advanced Settings Rav1e Mastering Display Bx
                        writer.WriteElementString("VideoAdvancedRav1eMasteringBy",  TextBoxRav1eMasteringBy.Text);                      // Video Advanced Settings Rav1e Mastering Display By
                        writer.WriteElementString("VideoAdvancedRav1eMasteringRx",  TextBoxRav1eMasteringRx.Text);                      // Video Advanced Settings Rav1e Mastering Display Rx
                        writer.WriteElementString("VideoAdvancedRav1eMasteringRy",  TextBoxRav1eMasteringRy.Text);                      // Video Advanced Settings Rav1e Mastering Display Ry
                        writer.WriteElementString("VideoAdvancedRav1eMasteringWPx", TextBoxRav1eMasteringWPx.Text);                     // Video Advanced Settings Rav1e Mastering Display WPx
                        writer.WriteElementString("VideoAdvancedRav1eMasteringWPy", TextBoxRav1eMasteringWPy.Text);                     // Video Advanced Settings Rav1e Mastering Display WPy
                        writer.WriteElementString("VideoAdvancedRav1eMasteringLx",  TextBoxRav1eMasteringLx.Text);                      // Video Advanced Settings Rav1e Mastering Display Lx
                        writer.WriteElementString("VideoAdvancedRav1eMasteringLy",  TextBoxRav1eMasteringLy.Text);                      // Video Advanced Settings Rav1e Mastering Display Ly
                    }
                    writer.WriteElementString("VideoAdvancedRav1eLight",        CheckBoxRav1eContentLight.IsChecked.ToString());        // Video Advanced Settings Rav1e Mastering Content Light
                    if (CheckBoxRav1eMasteringDisplay.IsChecked == true)
                    {
                        writer.WriteElementString("VideoAdvancedRav1eLightCll",  TextBoxRav1eContentLightCll.Text);                     // Video Advanced Settings Rav1e Mastering Content Light Cll
                        writer.WriteElementString("VideoAdvancedRav1eLightFall", TextBoxRav1eContentLightFall.Text);                    // Video Advanced Settings Rav1e Mastering Content Light Fall
                    }
                }
                else if (ComboBoxVideoEncoder.SelectedIndex == 2)
                {
                    // svt-av1
                    writer.WriteElementString("VideoAdvancedSVTAV1TileCols",    ComboBoxSVTAV1TileColumns.SelectedIndex.ToString());    // Video Advanced Settings SVT-AV1 Tile Columns
                    writer.WriteElementString("VideoAdvancedSVTAV1TileRows",    ComboBoxSVTAV1TileRows.SelectedIndex.ToString());       // Video Advanced Settings SVT-AV1 Tile Rows
                    writer.WriteElementString("VideoAdvancedSVTAV1GOP",         TextBoxSVTAV1MaxGOP.Text);                              // Video Advanced Settings SVT-AV1 GOP
                    writer.WriteElementString("VideoAdvancedSVTAV1AQMode",      ComboBoxSVTAV1AQMode.SelectedIndex.ToString());         // Video Advanced Settings SVT-AV1 AQ-Mode
                    writer.WriteElementString("VideoAdvancedSVTAV1ColorFmt",    ComboBoxSVTAV1ColorFormat.SelectedIndex.ToString());    // Video Advanced Settings SVT-AV1 Color Format
                    writer.WriteElementString("VideoAdvancedSVTAV1Profile",     ComboBoxSVTAV1Profile.SelectedIndex.ToString());        // Video Advanced Settings SVT-AV1 Profile
                    writer.WriteElementString("VideoAdvancedSVTAV1AltRefLevel", ComboBoxSVTAV1AltRefLevel.SelectedIndex.ToString());    // Video Advanced Settings SVT-AV1 Alt Ref Level
                    writer.WriteElementString("VideoAdvancedSVTAV1AltRefStren", ComboBoxSVTAV1AltRefStrength.SelectedIndex.ToString()); // Video Advanced Settings SVT-AV1 Alt Ref Strength
                    writer.WriteElementString("VideoAdvancedSVTAV1AltRefFrame", ComboBoxSVTAV1AltRefFrames.SelectedIndex.ToString());   // Video Advanced Settings SVT-AV1 Alt Ref Frames
                    writer.WriteElementString("VideoAdvancedSVTAV1HDR",         CheckBoxSVTAV1HDR.IsChecked.ToString());                // Video Advanced Settings SVT-AV1 HDR
                }

            }
            else if (CheckBoxVideoAdvancedSettings.IsChecked == true && CheckBoxCustomVideoSettings.IsChecked == true)
            {
                writer.WriteElementString("VideoAdvancedCustomString",          TextBoxCustomVideoSettings.Text);                       // Video Advanced Settings Custom String
            }

            // Writes Ending XML Element
            writer.WriteEndElement();
            // Cloeses XML Writer
            writer.Close();
            // Closes somehow this class instance
            this.Close();
        }
        
    }
}
