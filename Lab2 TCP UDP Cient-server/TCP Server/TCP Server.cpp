// ---------------- TCP SERVER ----------------
// Server receives an array of integers,
// sorts it, and sends the result back to the client
// Protocol: TCP (SOCK_STREAM)

#define _WINSOCK_DEPRECATED_NO_WARNINGS

#include <stdio.h>
#include <winsock2.h>

#pragma comment(lib, "ws2_32.lib")

// ----------------------------------------------------
// Partition array for QuickSort algorithm
// Returns pivot index
// ----------------------------------------------------
int partition(int arr[], int low, int high) {
    int pivot = arr[high];      // pivot element
    int i = low - 1;            // index of smaller element

    for (int j = low; j < high; j++) {
        if (arr[j] < pivot) {
            i++;
            // swap elements
            int temp = arr[i];
            arr[i] = arr[j];
            arr[j] = temp;
        }
    }

    // place pivot at correct position
    int temp = arr[i + 1];
    arr[i + 1] = arr[high];
    arr[high] = temp;

    return i + 1;
}

// ----------------------------------------------------
// Recursive quicksort
// ----------------------------------------------------
void quickSort(int arr[], int low, int high) {
    if (low < high) {
        int pi = partition(arr, low, high);
        quickSort(arr, low, pi - 1);
        quickSort(arr, pi + 1, high);
    }
}

int main() {
    WSADATA wsa;

    // Initialize Winsock library
    // Must be done before any socket API calls
    if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0) {
        printf("WSAStartup error\n");
        return 1;
    }

    // Create TCP socket
    // AF_INET      - IPv4
    // SOCK_STREAM  - stream socket (TCP)
    SOCKET sock = socket(AF_INET, SOCK_STREAM, 0);
    if (sock == INVALID_SOCKET) {
        printf("Socket creation error\n");
        WSACleanup();
        return 1;
    }

    struct sockaddr_in server;

    // Prepare local server address
    server.sin_family = AF_INET;
    server.sin_port = htons(8080);
    server.sin_addr.s_addr = inet_addr("127.0.0.1");

    // Bind socket to address and port
    if (bind(sock, (struct sockaddr*)&server, sizeof(server)) == SOCKET_ERROR) {
        printf("Bind error\n");
        closesocket(sock);
        WSACleanup();
        return 1;
    }

    // Set socket to listening mode
    listen(sock, 5);

    printf("TCP server started...\n");

    // Infinite loop to handle clients
    while (1) {

        SOCKET client;
        struct sockaddr_in clientAddr;
        int clientSize = sizeof(clientAddr);

        // Accept incoming connection
        client = accept(sock, (struct sockaddr*)&clientAddr, &clientSize);
        if (client == INVALID_SOCKET)
            continue;

        int n;
        int arr[100];

        while (1) {
            // Receive the size of the array
            int recvBytes = recv(client, (char*)&n, sizeof(int), 0);
            if (recvBytes <= 0 || n == 0) {

                // Client closed the connection or sent n=0
                printf("Client disconnected.\n");
                break;
            }

            // Receive the array
            recv(client, (char*)arr, n * sizeof(int), 0);

            // Sort the array
            quickSort(arr, 0, n - 1);

            // Display result on server side
            printf("Sorted array (server): ");
            for (int i = 0; i < n; i++)
                printf("%d ", arr[i]);
            printf("\n");

            // Send sorted array back to client
            send(client, (char*)arr, n * sizeof(int), 0);
        }

        // Close client connection
        shutdown(client, 0);
        closesocket(client);
    }

    closesocket(sock);
    WSACleanup();
    return 0;
}
