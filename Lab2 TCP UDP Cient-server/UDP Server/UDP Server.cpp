// ---------------- UDP SERVER ----------------
// Server receives an array of integers,
// sorts it, and sends the result back to the client
// Protocol: UDP (SOCK_DGRAM)

#define _WINSOCK_DEPRECATED_NO_WARNINGS

#include <stdio.h>
#include <winsock2.h>

#pragma comment(lib, "ws2_32.lib")

// ----------------------------------------------------
// Partition array for QuickSort algorithm
// Returns pivot index
// ----------------------------------------------------
int partition(int arr[], int low, int high) {
    int pivot = arr[high];
    int i = low - 1;

    for (int j = low; j < high; j++) {
        if (arr[j] < pivot) {
            i++;
            int temp = arr[i];
            arr[i] = arr[j];
            arr[j] = temp;
        }
    }

    int temp = arr[i + 1];
    arr[i + 1] = arr[high];
    arr[high] = temp;

    return i + 1;
}

// ----------------------------------------------------
// Recursive QuickSort
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
    if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0) {
        printf("WSAStartup error\n");
        return 1;
    }

    // Create UDP socket
    SOCKET sock = socket(AF_INET, SOCK_DGRAM, 0);
    if (sock == INVALID_SOCKET) {
        printf("Socket creation error\n");
        WSACleanup();
        return 1;
    }

    struct sockaddr_in server, client;
    int clientSize = sizeof(client);

    // Prepare local server address
    server.sin_family = AF_INET;
    server.sin_port = htons(8080);
    server.sin_addr.s_addr = inet_addr("127.0.0.1");

    // Bind socket to local address
    if (bind(sock, (struct sockaddr*)&server, sizeof(server)) == SOCKET_ERROR) {
        printf("Bind error\n");
        closesocket(sock);
        WSACleanup();
        return 1;
    }

    printf("UDP server started...\n");

    // Infinite loop to process incoming datagrams
    while (1) {
        int arr[100];
        int n;

        // Receive number of elements
        recvfrom(sock, (char*)&n, sizeof(int), 0,
            (struct sockaddr*)&client, &clientSize);

        // Receive array of integers
        recvfrom(sock, (char*)arr, n * sizeof(int), 0,
            (struct sockaddr*)&client, &clientSize);

        // Sort the array
        quickSort(arr, 0, n - 1);

        // Display result on server side
        printf("Sorted array (server): ");
        for (int i = 0; i < n; i++)
            printf("%d ", arr[i]);
        printf("\n");

        // Send sorted array back to client
        sendto(sock, (char*)arr, n * sizeof(int), 0,
            (struct sockaddr*)&client, clientSize);
    }

    closesocket(sock);
    WSACleanup();
    return 0;
}