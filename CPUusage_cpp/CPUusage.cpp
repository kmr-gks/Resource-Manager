#include <conio.h>
#include <stdio.h>
#include <stdlib.h>
#include <windows.h>
#include <pdh.h>    // Pdh.Lib
#pragma comment(lib, "pdh.lib")

// メイン関数
int main(void)
{
	PDH_HQUERY              hQuery;
	PDH_HCOUNTER            hCounter;
	PDH_FMT_COUNTERVALUE    fmtValue;

	wprintf(L"開始\n");

	if (PdhOpenQuery(NULL, 0, &hQuery) == ERROR_SUCCESS) {
		PdhAddCounter(hQuery, TEXT("\\Processor(_Total)\\% Processor Time"), 0, &hCounter);
		PdhCollectQueryData(hQuery);

		while (!_kbhit()) {
			Sleep(100);
			//system(TEXT("CLS"));
			PdhCollectQueryData(hQuery);
			// double型の表示
			PdhGetFormattedCounterValue(hCounter, PDH_FMT_DOUBLE, NULL, &fmtValue);
			wprintf(TEXT("\r doubleValue = %.15f"), fmtValue.doubleValue);
		}
		_getch();
		PdhCloseQuery(hQuery);
		return 0;
	}
	wprintf(TEXT("クエリーをオープンできません。\n"));
	return 255;
}