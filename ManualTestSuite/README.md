Manual Test Suite Guidelines:
1. Compile MainNode and Node solutions
2. Copy executable, dll, and other dependencies (may differ, for us it was MainNode/Node.runtimeconfig.json) to each of the MainNode and Node folders in the ManualTestSuite.
3. Start all the executables in each of the folders. The Main Node executables might ask for permission to access the network.
4. You may now simulate Nodes by typing in the Nodes' command line interface.


If you wish to test another graph, download it as .XML from dcrgraphs.net and insert it in the ManualTestSuite folder as "graph.xml"
You must then change all the .ini files in each folder - how to do this is described in depth in the How To Run section of the report.