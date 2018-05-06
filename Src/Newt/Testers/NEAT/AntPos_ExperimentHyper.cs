using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Game.HelperClassesAI;
using Game.HelperClassesWPF;
using SharpNeat.Core;
using SharpNeat.Decoders;
using SharpNeat.Decoders.HyperNeat;
using SharpNeat.Genomes.Neat;
using SharpNeat.Network;
using SharpNeat.Phenomes;

namespace Game.Newt.Testers.NEAT
{
    public class AntPos_ExperimentHyper : ExperimentNEATBase
    {
        #region Declaration Section

        private readonly double _inputSizeWorld;
        private readonly double _outputSizeWorld;

        //private NetworkActivationScheme _activationSchemeCppn = null;
        //private NetworkActivationScheme _activationScheme = null;

        #endregion

        #region Constructor

        public AntPos_ExperimentHyper(double inputSizeWorld, double outputSizeWorld)
        {
            _inputSizeWorld = inputSizeWorld;
            _outputSizeWorld = outputSizeWorld;
        }

        #endregion

        #region Public Methods

        // This is built from these two sources:
        //http://www.nashcoding.com/2010/10/29/tutorial-%E2%80%93-evolving-neural-networks-with-sharpneat-2-part-3/
        //SharpNeat.Domains.BoxesVisualDiscrimination.BoxesVisualDiscriminationExperiment.CreateGenomeDecoder()

        //TODO: Instead of hardcoding input and output as 2D squares, the constructor should take enums for common shapes { Line, Square, Cube, CirclePerimiter, CircleArea, SphereShell, SphereFilled }
        public IGenomeDecoder<NeatGenome, IBlackBox> CreateGenomeDecoder(int inputSizeXY, int outputSizeXY)
        {
            int numInputs = inputSizeXY * inputSizeXY;
            int numOutputs = outputSizeXY * outputSizeXY;

            SubstrateNodeSet inputLayer = new SubstrateNodeSet(numInputs);
            SubstrateNodeSet outputLayer = new SubstrateNodeSet(numOutputs);


            // Each node in each layer needs a unique ID
            // Node IDs start at 1. (bias node is always zero)
            // The input nodes use ID range { 1, inputSize^2 } and
            // the output nodes are the next outputSize^2.
            uint inputId = 1;
            uint outputId = Convert.ToUInt32(numInputs + 1);

            var inputCells = Math2D.GetCells_WithinSquare(_inputSizeWorld, inputSizeXY);
            var outputCells = Math2D.GetCells_WithinSquare(_outputSizeWorld, outputSizeXY);


            for (int y = 0; y < inputSizeXY; y++)
            {
                for (int x = 0; x < inputSizeXY; x++)
                {
                    inputId++;
                    outputId++;

                    Point inputPoint = inputCells[(y * inputSizeXY) + x].center;
                    Point outputPoint = outputCells[(y * outputSizeXY) + x].center;

                    inputLayer.NodeList.Add(new SubstrateNode(inputId, new double[] { inputPoint.X, inputPoint.Y, -1d }));
                    outputLayer.NodeList.Add(new SubstrateNode(outputId, new double[] { outputPoint.X, outputPoint.Y, 1d }));
                }
            }


            List<SubstrateNodeSet> nodeSetList = new List<SubstrateNodeSet>();
            nodeSetList.Add(inputLayer);
            nodeSetList.Add(outputLayer);


            //TODO: The two samples that I copied from have the same number of inputs and outputs.  This mapping may be too simplistic when the counts are different
            // Define a connection mapping from the input layer to the output layer.
            List<NodeSetMapping> nodeSetMappingList = new List<NodeSetMapping>();
            nodeSetMappingList.Add(NodeSetMapping.Create(0, 1, (double?)null));



            // Construct the substrate using a steepened sigmoid as the phenome's
            // activation function. All weights under 0.2 will not generate 
            // connections in the final phenome.
            Substrate substrate = new Substrate(nodeSetList, DefaultActivationFunctionLibrary.CreateLibraryCppn(), 0, 0.2, 5, nodeSetMappingList);



            // Create genome decoder. Decodes to a neural network packaged with
            // an activation scheme that defines a fixed number of activations per evaluation.
            IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder = new HyperNeatDecoder(substrate, _activationSchemeCppn, _activationScheme, false);



            return genomeDecoder;
        }

        #endregion
    }
}
