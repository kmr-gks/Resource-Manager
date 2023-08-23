#include <conio.h>
#include <stdio.h>
#include <stdlib.h>
#include <windows.h>
#include <pdh.h>    // Pdh.Lib
#pragma comment(lib, "pdh.lib")

// ���C���֐�
int main(void)
{
	PDH_HQUERY              hQuery;
	PDH_HCOUNTER            hCounter;
	PDH_FMT_COUNTERVALUE    fmtValue;

	wprintf(L"�J�n\n");

	if (PdhOpenQuery(NULL, 0, &hQuery) == ERROR_SUCCESS) {
		PdhAddCounter(hQuery, TEXT("\\Processor(_Total)\\% Processor Time"), 0, &hCounter);
		PdhCollectQueryData(hQuery);

		while (!_kbhit()) {
			Sleep(100);
			//system(TEXT("CLS"));
			PdhCollectQueryData(hQuery);
			// double�^�̕\��
			PdhGetFormattedCounterValue(hCounter, PDH_FMT_DOUBLE, NULL, &fmtValue);
			wprintf(TEXT("\r doubleValue = %.15f"), fmtValue.doubleValue);
		}
		_getch();
		PdhCloseQuery(hQuery);
		return 0;
	}
	wprintf(TEXT("�N�G���[���I�[�v���ł��܂���B\n"));
	return 255;
}