using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace INFOIBV
{
	public partial class INFOIBV : Form
	{
		private Bitmap InputImage;
		private Bitmap OutputImage;

		public INFOIBV()
		{
			InitializeComponent();
		}

		private void LoadImageButton_Click(object sender, EventArgs e)
		{
			if (openImageDialog.ShowDialog() == DialogResult.OK)             // Open File Dialog
			{
				string file = openImageDialog.FileName;                     // Get the file name
				imageFileName.Text = file;                                  // Show file name
				if (InputImage != null) InputImage.Dispose();               // Reset image
				InputImage = new Bitmap(file);                              // Create new Bitmap from file
				if (InputImage.Size.Height <= 0 || InputImage.Size.Width <= 0 ||
					InputImage.Size.Height > 512 || InputImage.Size.Width > 512) // Dimension check
					MessageBox.Show("Error in image dimensions (have to be > 0 and <= 512)");
				else
					pictureBox1.Image = (Image)InputImage;                 // Display input image
			}
		}

		private void applyButton_Click(object sender, EventArgs e)
		{
			if (InputImage == null) return;                                 // Get out if no input image
			if (OutputImage != null) OutputImage.Dispose();                 // Reset output image
			OutputImage = new Bitmap(InputImage.Size.Width, InputImage.Size.Height); // Create new output image
			Color[,] Image = new Color[InputImage.Size.Width, InputImage.Size.Height]; // Create array to speed-up operations (Bitmap functions are very slow)

			// Setup progress bar
			progressBar.Visible = true;
			progressBar.Minimum = 1;
			progressBar.Maximum = InputImage.Size.Width * InputImage.Size.Height;
			progressBar.Value = 1;
			progressBar.Step = 1;

			// Copy input Bitmap to array            
			for (int x = 0; x < InputImage.Size.Width; x++)
			{
				for (int y = 0; y < InputImage.Size.Height; y++)
				{
					Image[x, y] = InputImage.GetPixel(x, y);                // Set pixel color in array at (x,y)
				}
			}

			//==========================================================================================

			// Calculate a_low and a_high
			Color aLow = Color.FromArgb(255, 255, 255);                     // a-low default value
			Color aHigh = Color.FromArgb(0, 0, 0);                          // a-high default value
			CalculateAlowAndAhigh(Image, ref aLow, ref aHigh);              // Calculate a-low and a-high 

			// (1) Grayscale Conversion
			//GrayscaleConversion(Image);

			// (2) Contrast Adjustment
			//ContrastAdjustment(Image, aLow, aHigh);

			// (3) Gaussian Filter
			int kernelSize = 3;                                             // Size of the kernel
			float sigma = 1f;                                               // Sigma
			double[,] kernel = GaussianFilter(kernelSize, sigma);

			// (4) Linear Filtering
			// TODO: sneller maken? Hij doet het, maar bij hogere kernelgrootte word ie heel sloom
			LinearFiltering(Image, kernel);

			// (5) Nonlinear Filtering
			// TODO: Implement

			// (6) Edge Detection
			// TODO: Implement

			// (7) Thresholding
			int threshold = 130;
			//Thresholding(Image, threshold);

			//==========================================================================================

			// Copy array to output Bitmap
			for (int x = 0; x < InputImage.Size.Width; x++)
			{
				for (int y = 0; y < InputImage.Size.Height; y++)
				{
					OutputImage.SetPixel(x, y, Image[x, y]);               // Set the pixel color at coordinate (x,y)
				}
			}
			pictureBox2.Image = (Image)OutputImage;                         // Display output image
			progressBar.Visible = false;                                    // Hide progress bar
		}

		// (1) Grayscale Conversion
		private void GrayscaleConversion(Color[,] Image)
		{
			Console.WriteLine("(1) Grayscale Conversion Executed");
			for (int x = 0; x < InputImage.Size.Width; x++)
			{
				for (int y = 0; y < InputImage.Size.Height; y++)
				{
					Color pixelColor = Image[x, y];                                                 // Get the pixel color at coordinate (x,y)
					Color updatedColor = Color.FromArgb(pixelColor.R, pixelColor.R, pixelColor.R);  // Grab only the red value of that pixel
					Image[x, y] = updatedColor;                                                     // Set the new pixel color at coordinate (x,y)
					progressBar.PerformStep();                                                      // Increment progress bar
				}
			}
			progressBar.Visible = false;                                                            // Hide progress bar
		}

		// (2) Contrast Adjustment
		private void ContrastAdjustment(Color[,] Image, Color aLow, Color aHigh)
		{
			Console.WriteLine("(2) Contrast Adjustment Executed");
			for (int x = 0; x < InputImage.Size.Width; x++)
			{
				for (int y = 0; y < InputImage.Size.Height; y++)
				{
					Color pixelColor = Image[x, y];                                             // Get the pixel color at coordinate (x,y)
					int newColor = (pixelColor.R - aLow.R) * (255 / (aHigh.R - aLow.R));        // Calculate the new color based on a-low and a-high
					Color updatedColor = Color.FromArgb(newColor, newColor, newColor);          // Grab only the red value on that pixel
					Image[x, y] = updatedColor;                                                 // Set the new pixel color at coordinate (x,y)
					progressBar.PerformStep();                                                  // Increment progress bar		
				}
			}
			progressBar.Visible = false;                                                        // Hide progress bar
		}

		// (3) Gaussian Filter
		private double[,] GaussianFilter(int kernelSize, float sigma)
		{
			Console.WriteLine("(3) Gaussian Filter Executed");

			double[,] gaussianKernel = new double[kernelSize, kernelSize];  // Create the kernel array
			int center = (kernelSize - 1) / 2;                              // Calculate the center position of the kernel
			double constant = 1d / (2 * Math.PI * sigma * sigma);           // Precalculate some stuff to prevent repeating calculations

			double sum = 0;                                                 // Keep track of the total of all the kernel values
			for (int x = 0; x < kernelSize; x++)
			{
				for (int y = 0; y < kernelSize; y++)
				{
					int xDist = x - center;
					int yDist = y - center;
					gaussianKernel[x, y] = /*constant **/ Math.Exp(-(xDist * xDist + yDist * yDist) / (2 * sigma * sigma));
					sum += gaussianKernel[x, y];
					progressBar.PerformStep();                              // Increment progress bar	
				}
			}

			double factor = 1 / sum;                                        // Create a factor to multiple with each kernel field
			double newSum = 0;
			for (int x = 0; x < kernelSize; x++)
			{
				for (int y = 0; y < kernelSize; y++)
				{
					gaussianKernel[x, y] = factor * gaussianKernel[x, y];   // Normalize the kernel so all values add up to 1
					newSum += gaussianKernel[x, y];                         // Add up all values to check if the sum is equal to 1
				}
			}

			progressBar.Visible = false;                                    // Hide progress bar

			// *** Just some printing to the console to check the values of the kernel *** //

			// Write the sum of all the kernel values to the console
			Console.WriteLine("Kernel values sum: " + newSum);

			// Display kernel values in console as a matrix
			Console.WriteLine("Kernel values matrix:");
			var rowCount = gaussianKernel.GetLength(0);
			var colCount = gaussianKernel.GetLength(1);
			for (int row = 0; row < rowCount; row++)
			{
				for (int col = 0; col < colCount; col++)
					Console.Write(String.Format("{0}\t", gaussianKernel[row, col]));
				Console.WriteLine();
			}

			return gaussianKernel;
		}

		// (4) Linear Filtering
		private void LinearFiltering(Color[,] Image, double[,] kernel)
		{
			int kernelSize = (int)Math.Sqrt(kernel.Length);
			int edgeDistance = kernelSize / 2;

			Console.WriteLine("(4) Linear Filtering Executed");
			for (int x = edgeDistance; x < InputImage.Size.Width - edgeDistance - 1; x++)
			{
				for (int y = edgeDistance; y < InputImage.Size.Height - edgeDistance - 1; y++)
				{
					double newColor = 0;
					Color pixelColor = Image[x, y];                                                             // Get the pixel color at coordinate (x,y)

					for (int u = -edgeDistance; u < edgeDistance; u++)
					{
						for (int v = -edgeDistance; v < edgeDistance; v++)
						{
							newColor += kernel[u + edgeDistance, v + edgeDistance] * Image[x + u, y + v].R;     // Calculate the new color value with the kernel
							progressBar.PerformStep();                                                          // Increment progress bar	
						}
					}

					Color updatedColor = Color.FromArgb((int)newColor, (int)newColor, (int)newColor);           // Grab only the red value on that pixel
					Image[x, y] = updatedColor;                                                                 // Set the new pixel color at coordinate (x,y)
				}
			}
			progressBar.Visible = false;                                                                        // Hide progress bar
		}

		// (7) Thresholding
		private void Thresholding(Color[,] Image, int threshold)
		{
			Console.WriteLine("(7) Thresholding Executed");
			for (int x = 0; x < InputImage.Size.Width; x++)
			{
				for (int y = 0; y < InputImage.Size.Height; y++)
				{
					Color pixelColor = Image[x, y];                                             // Get the pixel color at coordinate (x,y)
					int newColor = pixelColor.R;                                                // Calculate the new color based on the passed threshold value
					if (newColor < threshold)                                                   // Color value lower than threshold -> black
						newColor = 0;
					else                                                                        // Color value higher than threshold -> white
						newColor = 255;
					Color updatedColor = Color.FromArgb(newColor, newColor, newColor);          // Update new color value
					Image[x, y] = updatedColor;                                                 // Set the new pixel color at coordinate (x,y)
					progressBar.PerformStep();                                                  // Increment progress bar	
				}
			}
			progressBar.Visible = false;                                                        // Hide progress bar
		}

		private void CalculateAlowAndAhigh(Color[,] Image, ref Color aLow, ref Color aHigh)
		{
			// Loop through all pixels and find a-low and a-high
			for (int x = 0; x < InputImage.Size.Width; x++)
			{
				for (int y = 0; y < InputImage.Size.Height; y++)
				{
					Color pixelColor = Image[x, y];
					if (aLow.R > pixelColor.R)
						aLow = pixelColor;
					if (aHigh.R < pixelColor.R)
						aHigh = pixelColor;
					progressBar.PerformStep();                                                  // Increment progress bar
				}
			}
			progressBar.Visible = false;                                                        // Hide progress bar
		}

		private void saveButton_Click(object sender, EventArgs e)
		{
			if (OutputImage == null) return;                                // Get out if no output image
			if (saveImageDialog.ShowDialog() == DialogResult.OK)
				OutputImage.Save(saveImageDialog.FileName);                 // Save the output image
		}

		private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
		{

		}
	}
}
