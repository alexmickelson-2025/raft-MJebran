# Test Cases for Raft Protocol

## Leader Heartbeat
- **Test Case:** When a leader is active, it sends a heartbeat within 50ms.
  - **Steps:**
    1. Create a node and elect it as the leader.
    2. Start a timer to monitor heartbeats.
    3. Assert that a heartbeat message is sent within 50ms.
  - **Expected Outcome:** Heartbeat is sent within the specified time.

## AppendEntries and Leader Recognition
- **Test Case:** When a node receives an `AppendEntries` from another node, it remembers that node as the current leader.
  - **Steps:**
    1. Initialize a node in follower state.
    2. Send an `AppendEntries` RPC from another node.
    3. Check the follower's state for the leader ID.
  - **Expected Outcome:** The follower recognizes the sender as the leader.

## Node Initialization
- **Test Case:** When a new node is initialized, it should be in the follower state.
  - **Steps:**
    1. Create a new node.
    2. Check its initial state.
  - **Expected Outcome:** The state of the node is "follower."

## Election Timer Expiry
- **Test Case:** When a follower doesn't get a message for 300ms, it starts an election.
  - **Steps:**
    1. Create a follower node.
    2. Simulate no incoming messages for 300ms.
    3. Assert that the node starts an election.
  - **Expected Outcome:** Election is started after 300ms.

## Randomized Election Timeout
- **Test Case:** When the election timeout is reset, it is a random value between 150ms and 300ms.
  - **Steps:**
    1. Reset the election timer multiple times.
    2. Log the timeout values.
    3. Assert that the values are within the range [150ms, 300ms].
    4. Ensure randomness by checking for variability across multiple calls.
  - **Expected Outcome:** Randomized values between 150ms and 300ms.

## Term Incrementation
- **Test Case:** When a new election begins, the term is incremented by 1.
  - **Steps:**
    1. Initialize a new node and store its current term.
    2. Wait for 300ms to trigger an election.
    3. Check the new term.
  - **Expected Outcome:** The term is incremented by at least 1.

## Election Timer Reset on AppendEntries
- **Test Case:** When a follower receives an `AppendEntries` message, it resets the election timer.
  - **Steps:**
    1. Create a follower node and start an election timer.
    2. Send an `AppendEntries` RPC before 300ms.
    3. Ensure no election starts after 300ms.
  - **Expected Outcome:** Election timer resets, and no election occurs.

## Leader Election on Majority Votes
- **Test Case:** Given an election begins, when the candidate gets a majority of votes, it becomes a leader.
  - **Steps:**
    1. Start an election with multiple nodes.
    2. Simulate a majority vote for the candidate.
    3. Check the candidate's state.
  - **Expected Outcome:** The candidate becomes a leader.

## Handling Unresponsive Nodes During Election
- **Test Case:** Given a candidate receives a majority of votes while waiting for an unresponsive node, it still becomes a leader.
  - **Steps:**
    1. Create a cluster with one unresponsive node.
    2. Conduct an election and simulate majority votes.
    3. Check the state of the candidate.
  - **Expected Outcome:** The candidate becomes a leader.

## Voting for Previous Term
- **Test Case:** A follower that has not voted and is in an earlier term responds to a `RequestForVoteRPC` with yes.
  - **Steps:**
    1. Create a follower in an earlier term.
    2. Send a `RequestForVoteRPC` from a candidate in a later term.
    3. Check the response.
  - **Expected Outcome:** The follower votes yes.

## Candidate Self-Voting
- **Test Case:** Given a candidate server that just became a candidate, it votes for itself.
  - **Steps:**
    1. Transition a follower to a candidate.
    2. Check the vote count.
  - **Expected Outcome:** The candidate votes for itself.

## AppendEntries Handling
- **Test Case 1:** Given a candidate receives an `AppendEntries` message from a node with a later term, it becomes a follower.
- **Test Case 2:** Given a candidate receives an `AppendEntries` message from a node with an equal term, it becomes a follower.
- **Steps:**
    1. Create a candidate node.
    2. Send `AppendEntries` RPCs with later and equal terms.
    3. Check the candidate's state after each RPC.
  - **Expected Outcome:** The candidate transitions to follower.

## Voting Logic
- **Test Case 1:** If a node receives a second request for a vote for the same term, it should respond no.
- **Test Case 2:** If a node receives a second request for a vote for a future term, it should vote for that node.
  - **Steps:**
    1. Send multiple `RequestForVoteRPC`s for the same and future terms.
    2. Check the responses.
  - **Expected Outcome:** Correct responses based on term and voting status.

## New Election on Timer Expiry
- **Test Case:** Given a candidate, when an election timer expires inside an election, a new election is started.
  - **Steps:**
    1. Start an election with a candidate.
    2. Wait for the timer to expire.
    3. Check the election state and term.
  - **Expected Outcome:** A new election is initiated with an incremented term.

## AppendEntries Response
- **Test Case:** When a follower node receives an `AppendEntries` request, it sends a response.
  - **Steps:**
    1. Send an `AppendEntries` RPC to a follower.
    2. Check for the response.
  - **Expected Outcome:** The follower responds to the request.

## Candidate Rejection of Old Term
- **Test Case:** Given a candidate receives an `AppendEntries` from a previous term, it rejects the request.
  - **Steps:**
    1. Send an `AppendEntries` RPC with an earlier term to a candidate.
    2. Check the response.
  - **Expected Outcome:** The candidate rejects the request.

## Leader's Immediate Heartbeat
- **Test Case:** When a candidate wins an election, it immediately sends a heartbeat.
  - **Steps:**
    1. Simulate a successful election.
    2. Check for heartbeat messages.
  - **Expected Outcome:** Heartbeat is sent immediately after becoming a leader.

## Notes
- All RPCs should be implemented as asynchronous calls to simulate real-world distributed systems behavior.