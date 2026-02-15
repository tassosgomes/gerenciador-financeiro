import { RouterProvider } from 'react-router-dom';

import { router } from '@/app/router/routes';

function App(): JSX.Element {
  return <RouterProvider router={router} />;
}

export default App;
