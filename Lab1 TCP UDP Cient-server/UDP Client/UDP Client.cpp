// ---------------- UDP CLIENT ----------------
// Client enters an array of integers,
// sends it to the server, and receives the sorted result
// Protocol: UDP (SOCK_DGRAM)

#define _WINSOCK_DEPRECATED_NO_WARNINGS
#define _CRT_SECURE_NO_WARNINGS

#include <stdio.h>
#include <winsock2.h>

#pragma comment(lib, "ws2_32.lib")

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

    struct sockaddr_in server;

    // Prepare server address
    server.sin_family = AF_INET;
    server.sin_port = htons(8080);
    server.sin_addr.s_addr = inet_addr("127.0.0.1");

    char choice;

    do {
        int arr[100];
        int n;

        // User input
        printf("Enter the number of array elements: ");
        scanf("%d", &n);

        printf("Enter array elements:\n");
        for (int i = 0; i < n; i++)
            scanf("%d", &arr[i]);

        // Send number of elements
        sendto(sock, (char*)&n, sizeof(int), 0,
            (struct sockaddr*)&server, sizeof(server));

        // Send array to server
        sendto(sock, (char*)arr, n * sizeof(int), 0,
            (struct sockaddr*)&server, sizeof(server));

        // Receive sorted array
        recvfrom(sock, (char*)arr, n * sizeof(int), 0, NULL, NULL);

        // Display result
        printf("Sorted array (client): ");
        for (int i = 0; i < n; i++)
            printf("%d ", arr[i]);
        printf("\n");

        printf("Continue? (y/n): ");
        scanf(" %c", &choice);

    } while (choice == 'y');

    closesocket(sock);
    WSACleanup();
    return 0;
}