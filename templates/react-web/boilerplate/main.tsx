import { createRoot } from 'react-dom/client';
import { ThemeProvider } from 'styled-components';
import { theme } from './theme';

const container = document.getElementById('root');
if (container) {
  createRoot(container).render(
    <ThemeProvider theme={theme}>
      <div />
    </ThemeProvider>,
  );
}
