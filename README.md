# Peer-to-peer-DCR-Graph-engine
Created in collaboration with Andreas Nienstædt Halling Larsen

The P2P system consists of two parts, each of which have their own CLI and unit tests.
1. Main Nodes, which each contain a local part of the global DCR graph and are port-forwarded. These listen for incoming requests from their associated Nodes, and in response retrieve and manipulate local data, as well as data on other Main Nodes, through asynchronous messaging and propagation across Main Nodes.
2. Nodes, which each communicate exclusively with their own Main Node by requesting executions of events local to that Main Node, as well as information about both the local and global graph.

A manual testing suite is attached as an example of how to set up the system. Refer to the thesis pdf for more detailed information on the project, as well as how to set up the system.
